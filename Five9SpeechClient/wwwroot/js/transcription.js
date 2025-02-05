"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("https://localhost:7117/transcriptionhub").build();

connection.on("Recognizing", function (text) {
    var div = document.getElementById("recognizingText");
    div.textContent = `${text}`;
});

connection.on("Recognized", function (text) {
    var div = document.getElementById("recognizingText");
    div.textContent = '';
    var li = document.createElement("li");
    li.classList.add("list-group-item");
    // Pay attention to possible script injection concerns.
    li.textContent = `${text}`;
    document.getElementById("recognizedText").appendChild(li);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});