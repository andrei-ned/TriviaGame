var protocol = location.protocol === "https:" ? "wss:" : "ws:";
var wsUri = protocol + "//" + window.location.host + "/gamehub";
var connection = new signalR.HubConnectionBuilder().withUrl("/gamehub").build();

var secondsPerQuestion;

var myAnswer = -1;

addPlayer(username);

connection.start().then(function () {
    console.log("Connection started.");
    connection.invoke("InitUser", username);
});

connection.on("ReceiveGameData", function (secsPerQ) {
    console.log("Received game data");

    secondsPerQuestion = secsPerQ - 1; // Substract a second so late answers aren't lost due to latency
    questionCount = qCount;
})

connection.on("ReceiveChatMessage", function (user, message) {
    console.log("Received message: " + message + " from " + user);

    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var encodedMsg = "<strong>" + user + ":</strong> " + msg;
    var msgElement = document.createElement("p");
    msgElement.innerHTML = encodedMsg;
    msgElement.setAttribute("class", "text-break m-0");
    var chatboxMessages = document.getElementById("chatboxMessages");
    var shouldScroll = false;
    if (chatboxMessages.scrollHeight - chatboxMessages.offsetHeight < chatboxMessages.scrollTop + 5) {
        shouldScroll = true;
    }
    console.log(chatboxMessages.scrollHeight - chatboxMessages.offsetHeight);
    chatboxMessages.appendChild(msgElement);
    if (shouldScroll) {
        msgElement.scrollIntoView(false);
    }
});

connection.on("ReceiveQuestionResults", function (playerResults, correctAnswer) {
    console.log("Received question results (correct answer: " + correctAnswer + "):");
    console.log(playerResults);

    var scoreUpdatesElem = $('#scoreUpdates');
    scoreUpdatesElem.show();
    scoreUpdatesElem.empty();

    playerResults.sort(function (a, b) {
        return a.scoreThisQuestion > b.scoreThisQuestion ? -1 : 1;
    });

    playerResults.forEach((player) => {
        // Update scoreboard
        updatePlayerScore(player.name, player.score);
        // Show picked answer
        $(".ac" + player.answerId).append(player.name + " ");
        // Change bg in scoreboard
        if (player.answerId != -1)
            $("." + player.name).addClass(player.answerId == correctAnswer ? "bgCorrect" : "bgWrong");
        // Show points earned this round
        scoreUpdatesElem.append("<p>" + player.name + " +" + player.scoreThisQuestion + "</p>");
    });

    // Show correct answer
    var btnCorrect = $("#answer" + correctAnswer);
    btnCorrect.removeClass("btn-outline-primary btn-primary");
    btnCorrect.addClass("btn-success");

    if (myAnswer != correctAnswer) {
        var btnWrong = $("#answer" + myAnswer);
        btnWrong.removeClass("btn-outline-primary btn-primary");
        btnWrong.addClass("btn-danger");
    }

    progressBarTransition(1, 100);

    sortScoreboard();
});

connection.on("ReceivePlayerAnswered", function (user) {
    console.log("Received: " + user + " gave an answer");
    $("." + user).addClass("bgAnswer");
});

connection.on("ReceiveNewPlayer", function (user, score) {
    console.log("Received new player: " + user);
    addPlayer(user);
    updatePlayerScore(user, score);
});

function updatePlayerScore(user, score) {
    $("." + user + " .score").html(score);
}

function addPlayer(user) {
    var playerElement = document.createElement("div");
    playerElement.setAttribute("class", user + " scoreboardEntry");
    var playerName = document.createElement("span");
    playerName.innerHTML = user;
    var playerScore = document.createElement("span");
    playerScore.innerHTML = "0";
    playerScore.setAttribute("class", "float-right score");

    playerElement.appendChild(playerName);
    playerElement.appendChild(playerScore);

    $('#scoreboard').append(playerElement);
}

connection.on("ReceivePlayerDisconnect", function (user) {
    console.log("Player disconnect: " + user);

    $("." + user).remove();
    sortScoreboard();
});

connection.on("ReceiveQuestion", function (question, qIndex, qCount, elapsed) {
    console.log("Received question " + qIndex + ":");
    console.log(question);

    $("#question").html(question.question);
    for (i = 0; i < question.answers.length; i++) {
        var btn = $("#answer" + i);
        btn.html(question.answers[i]);
        btn.attr("disabled", false);
        btn.removeClass("btn-primary active btn-danger btn-success");
        btn.addClass("btn-outline-primary");
    }

    myAnswer = -1;

    let transitionSecs = secondsPerQuestion - elapsed;
    console.log("Elapsed: " + elapsed + ", transitionsSecs: " + transitionSecs);
    progressBarTransition(transitionSecs, 0);
    $(".questionCounter").html("Question " + qIndex + "/" + qCount);

    console.log("hi");
    $('.scoreboardEntry').removeClass("bgAnswer bgCorrect bgWrong");
    $('#scoreUpdates').hide();
    $('.answerCaption').empty();
})

$('#chatboxInput').keypress(function (e) {
    if (e.which != 13)
        return;

    e.preventDefault();

    var message = $('#chatboxInput').val();
    connection.invoke("SendChatMessage", username, message).catch(function (err) {
        return console.error(err.toString());
    });

    console.log("Sent message: " + message);

    $('#chatboxInput').val('');
});

function submitAnswer(answerId) {
    console.log("Submitted answer " + answerId);

    connection.invoke("SendQuestionAnswer", answerId).catch(function (err) {
        return console.error(err.toString());
    });

    for (i = 0; i < 4; i++) {
        var btn = $("#answer" + i);
        btn.attr("disabled", true);
        if (answerId == i) {
            btn.removeClass("btn-outline-primary");
            btn.addClass("btn-primary active");
        }
        else {
            
            btn.addClass("btn-outline-primary");
            btn.removeClass("btn-primary active");
        }
    }

    myAnswer = answerId;
}

function idExists(id) {
    return ($(id).length) > 0;
}

function sortScoreboard() {
    var children = $('#scoreboard div');
    children.sort(function (a, b) {
        return parseInt($(a).children('.score').html()) >
            parseInt($(b).children('.score').html()) ? -1 : 1;
    });
    $('#scoreboard').append(children);
}

function progressBarTransition(secs, width) {
    $(".progress-bar").css("transition", "width " + secs + "s linear");
    $(".progress-bar").css("width", width + "%");
}