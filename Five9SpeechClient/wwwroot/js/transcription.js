"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${ADAPTER_BASE_URL}/transcriptionhub`)
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

connection.on("Recognizing", function (text) {
    const textContainer = document.getElementById("recognizedText");
    if (textContainer.children.length === 0) {
        //console.log("creating first P");
        var p = document.createElement("p");
        p.classList.add("fst-italic");
        p.textContent = text;
        textContainer.appendChild(p);
        p.scrollIntoView();
        return;
    }
    const lastP = textContainer.children[textContainer.children.length - 1];
    if (lastP.classList.contains("fst-italic")) {
        //console.log("updating existing P");
        lastP.textContent = text;
        lastP.scrollIntoView();
        return;
    } else {
        //console.log("creating another P");
        var p = document.createElement("p");
        p.classList.add("fst-italic");
        p.textContent = text;
        textContainer.appendChild(p);
        lastP.scrollIntoView();
        return;
    }
});

connection.on("Recognized", function (text, offsetInTicks, speakerId) {
    //console.log(`Recognized: ${text}`);
    const textContainer = document.getElementById("recognizedText");
    
    for (let childP of textContainer.children) {
        if (childP.classList.contains("fst-italic")) childP.remove();
    }

    const speaker = speakerId === 'Guest-1' ? 'Agent' : (speakerId === 'Guest-2' ? 'Caller' : speakerId.replace("Guest", "Caller"));
    // 1 tick = 100 nanoseconds
    const offset = offsetInTicks / 10000000;
    const mins = Math.floor(offset / 60);
    const secs = Math.floor(offset % 60);

    const p = document.createElement("p");
    p.textContent = `[${mins < 10 ? '0' + mins : mins}:${secs < 10 ? '0' + secs : secs}, ${speaker}]: ${text}`;
    textContainer.appendChild(p);
    p.scrollIntoView();
    return;
});

connection.start().then(
    function () {
        console.log(`registering for ${CALL_ID} transcription`);
        connection.invoke("RegisterForTranscript", `${CALL_ID}`);
    }
).catch(function (err) {
    return console.error(err.toString());
});