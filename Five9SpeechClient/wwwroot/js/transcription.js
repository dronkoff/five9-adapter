"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("https://localhost:7117/transcriptionhub").build();

connection.on("TranscriptionEvent", function (eventName, text) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    // Pay attention to possible script injection concerns.
    li.innerHTML = eventName === 'Recognized' ? `<b>${eventName}</b>: ${text}` : `${eventName}: ${text}`;
    
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

//document.getElementById("sendButton").addEventListener("click", function (event) {
//    var user = document.getElementById("userInput").value;
//    var message = document.getElementById("messageInput").value;
//    connection.invoke("SendMessage", user, message).catch(function (err) {
//        return console.error(err.toString());
//    });
//    event.preventDefault();
//});