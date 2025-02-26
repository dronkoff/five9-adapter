"use strict";

var connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7117/transcriptionhub")
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

connection.on("Recognizing", function (text) {
    //var div = document.getElementById("recognizingText");
    //div.textContent = `${text}`;

    var textContainer = document.getElementById("recognizedText");
    if (textContainer.children.length === 0) {
        console.log("creating first P");
        var p = document.createElement("p");
        p.classList.add("fst-italic");
        p.textContent = text;
        textContainer.appendChild(p);
        p.scrollIntoView();
        return;
    }
    var lastP = textContainer.children[textContainer.children.length-1];
    if (lastP.classList.contains("fst-italic")) {
        console.log("updating existing P");
        lastP.textContent = text;
        lastP.scrollIntoView();
        return;
    } else {
        console.log("creating another P");
        var p = document.createElement("p");
        p.classList.add("fst-italic");
        p.textContent = text;
        textContainer.appendChild(p);
        lastP.scrollIntoView();
        return;
    }
});

connection.on("Recognized", function (text) {
//    var div = document.getElementById("recognizingText");
//    div.textContent = '';
//    var li = document.createElement("li");
//    li.classList.add("list-group-item");
//    li.textContent = `${text}`;
//    document.getElementById("recognizedText").appendChild(li);

    var textContainer = document.getElementById("recognizedText");
    var lastP = textContainer.children[textContainer.children.length - 1];
    if (!lastP) console.warn("there should be a paragraph here");
    lastP.classList.remove("fst-italic")
    lastP.textContent = text;
});

connection.start().then(
    function () {
        console.log(`registering for ${CALL_ID} transcription`);
        connection.invoke("RegisterForTranscript", `${CALL_ID}`);
    }
).catch(function (err) {
    return console.error(err.toString());
});