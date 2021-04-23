var protocol = location.protocol === "https:" ? "wss:" : "ws:";
var wsUri = protocol + "//" + window.location.host + "/gamehub";
var connection = new signalR.HubConnectionBuilder().withUrl("/gamehub").build();

var secondsPerQuestion;
var secondsBetweenQuestions;
var currentQuestionIndex;
var totalQuestions;

var myAnswer = -1;
var readyStatus = false;

connection.start().then(function () {
    connection.invoke("InitUser", username);
});

connection.on("ReceiveGameData", function (secsPerQ, secsBetweenQ, players, isGameRunning) {
    secondsPerQuestion = secsPerQ - 1; // Substract a second so late answers aren't lost due to latency
    secondsBetweenQuestions = secsBetweenQ - 1;

    if (!isGameRunning) {
        $(".playing").hide();
        $(".end").hide();
        $(".start").show();

        players.forEach((player) => {
            readyPlayer(player.id, player.isReady);
        });
    }
})

connection.on("ReceiveChatMessage", function (name, message) {
    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var encodedMsg = "<strong>" + name + ":</strong> " + msg;
    var msgElement = document.createElement("p");
    msgElement.innerHTML = encodedMsg;
    msgElement.setAttribute("class", "text-break m-0");
    var chatboxMessages = document.getElementById("chatboxMessages");
    var shouldScroll = false;
    if (chatboxMessages.scrollHeight - chatboxMessages.offsetHeight < chatboxMessages.scrollTop + 5) {
        shouldScroll = true;
    }
    chatboxMessages.appendChild(msgElement);
    if (shouldScroll) {
        msgElement.scrollIntoView(false);
    }
});

connection.on("ReceiveQuestionResults", function (players, correctAnswer) {
    var scoreUpdatesElem = $('#scoreUpdates');
    scoreUpdatesElem.show();
    scoreUpdatesElem.empty();

    players.sort(function (a, b) {
        return a.scoreThisQuestion > b.scoreThisQuestion ? -1 : 1;
    });

    players.forEach((player) => {
        // Update scoreboard
        updatePlayerScore(player.id, player.score);
        // Show picked answer
        $(".ac" + player.answerId).append(player.name + " ");
        // Change bg in scoreboard
        if (player.answerId != -1)
            $("." + player.id).addClass(player.answerId == correctAnswer ? "bgCorrect" : "bgWrong");
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

    if (currentQuestionIndex != totalQuestions)
        progressBarTransition(secondsBetweenQuestions, 100);
    else
        progressBarTransition(1, 1);

    // Disable buttons
    for (i = 0; i < 4; i++) {
        $("#answer" + i).attr("disabled", true);
    }

    sortScoreboard();
});

connection.on("ReceivePlayerAnswered", function (playerId) {
    $("." + playerId).addClass("bgAnswer");
});

connection.on("ReceiveNewPlayer", function (player) {
    addPlayer(player);
    updatePlayerScore(player.id, player.score);
});

connection.on("UpdateScores", function (players) {
    players.forEach((player) => {
        updatePlayerScore(player.id, player.score);
    });
})

function updatePlayerScore(user, score) {
    $("." + user + " .score").html(score);
}

function addPlayer(player) {
    var playerElement = document.createElement("div");
    playerElement.setAttribute("class", player.id + " scoreboardEntry");
    var playerName = document.createElement("span");
    playerName.innerHTML = player.name;
    var playerScore = document.createElement("span");
    playerScore.innerHTML = "0";
    playerScore.setAttribute("class", "float-right score");

    playerElement.appendChild(playerName);
    playerElement.appendChild(playerScore);

    $('#scoreboard').append(playerElement);
}

connection.on("ReceivePlayerDisconnect", function (playerId) {
    $("." + playerId).remove();
    sortScoreboard();
});

connection.on("ReceiveQuestion", function (question, qIndex, qCount, elapsed) {
    $("#question").html(question.question);
    for (i = 0; i < question.answers.length; i++) {
        var btn = $("#answer" + i);
        btn.html(question.answers[i]);
        btn.attr("disabled", false);
        btn.removeClass("btn-primary active btn-danger btn-success");
        btn.addClass("btn-outline-primary");
    }

    myAnswer = -1;
    currentQuestionIndex = qIndex;
    totalQuestions = qCount;

    let transitionSecs = secondsPerQuestion - elapsed;
    progressBarTransition(transitionSecs, 0);
    $(".questionCounter").html("Question " + qIndex + "/" + qCount);

    $(".playing").show();
    $(".end").hide();
    $(".start").hide();

    $('.scoreboardEntry').removeClass("bgAnswer bgCorrect bgWrong");
    $('#scoreUpdates').hide();
    $('.answerCaption').empty();
})

connection.on("ReceiveGameEnd", function (secsUntilNext, players) {
    progressBarTransition(secsUntilNext - 1, 100);

    players.sort(function (a, b) {
        return a.scoreThisQuestion > b.scoreThisQuestion ? -1 : 1;
    });

    $(".end").empty();

    players.forEach((player, i) => {
        let h = i + 1;
        if (h > 4)
            h = 4;

        $(".end").append('<h' + h + '><span class="endEntry">' + player.name + '</span></h' + h + '>');
    });

    $(".playing").hide();
    $(".end").show();
    $(".start").hide();
})

$('#chatboxInput').keypress(function (e) {
    if (e.which != 13)
        return;

    e.preventDefault();

    var message = $('#chatboxInput').val();
    connection.invoke("SendChatMessage", username, message).catch(function (err) {
        return console.error(err.toString());
    });

    $('#chatboxInput').val('');
});

function submitAnswer(answerId) {
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

function ready() {
    readyStatus = !readyStatus;

    $(".btn-ready").html(readyStatus ? "Unready" : "Ready");

    connection.invoke("SendReady", readyStatus).catch(function (err) {
        return console.error(err.toString());
    });
}

connection.on("ReceivePlayerReady", function (playerId, isReady) {
    readyPlayer(playerId, isReady);
})

function readyPlayer(playerId, isReady) {
    if (isReady)
        $("." + playerId).addClass("bgAnswer");
    else
        $("." + playerId).removeClass("bgAnswer");
}