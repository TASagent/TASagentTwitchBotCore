// Convert time to a format of hours, minutes, seconds, and milliseconds


let connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/Timer")
    .build();

let startTime;
let elapsedTime = 0;
let lapTime = 0;
let lapTimeString = "";
let timerInterval;

let mainDisplayMode = 0;
let secondaryDisplayMode = 0;

let mainDisplayTextElement = $("#text-MainDisplay");
let secondaryDisplayTextElement = $("#text-SecondaryDisplay");

function GetDisplayText(displayMode)
{
    switch (displayMode) {
        //None
        case 0: return "";

        //Cumulative
        case 1: return timeToString(elapsedTime + lapTime);

        //Current
        case 2: return timeToString(elapsedTime);

        //Lap Start
        case 3: return lapTimeString;

        default: return "00:00:00";
    }
}

function Print() {
    mainDisplayTextElement.text(GetDisplayText(mainDisplayMode));
    secondaryDisplayTextElement.text(GetDisplayText(secondaryDisplayMode));
}

function Start() {
    startTime = Date.now() - elapsedTime;
    timerInterval = setInterval(function() {
        elapsedTime = Date.now() - startTime;
        Print();
    }, 10);
}

function SetState(stateValue)
{
    clearInterval(timerInterval);
    $("#text-MainLabel").text(stateValue.layout.mainLabel);
    $("#text-SecondaryLabel").text(stateValue.layout.secondaryLabel);
    mainDisplayMode = stateValue.layout.mainDisplay;
    secondaryDisplayMode = stateValue.layout.secondaryDisplay;

    lapTime = 0;
    stateValue.laps.forEach(function(value) { lapTime += value; });
    lapTimeString = timeToString(lapTime);

    elapsedTime = stateValue.currentMS;

    if (stateValue.ticking)
    {
        Start();
    }
    else
    {
        Print();
    }
}

connection.on('SetState', SetState);

async function Initiate()
{
    await connection.start();
    await connection.invoke("RequestState");
}

Initiate();