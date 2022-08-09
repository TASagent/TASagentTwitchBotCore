// Convert time to a format of hours, minutes, seconds, and milliseconds

let connection = new signalR.HubConnectionBuilder()
  .withUrl("/Hubs/Donation")
  .build();

let formatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD'
});

let goalAmount = 2000;
let currentAmount = 0;
let displayedAmount = 0;
let updateInterval;
let lastUpdate = Date.now();

//Immediately set the state
function SetState(data) {
  currentAmount = data.newAmount;
  displayedAmount = data.newAmount;
  goalAmount = data.newGoal;
  SetText();
}

//Sets the current funding amount
function UpdateAmount(data) {
  currentAmount = data.newAmount;
  AnimateText();
}

function AnimateText() {
  clearInterval(updateInterval);
  //Calculate rate

  if (displayedAmount >= currentAmount) {
    displayedAmount = currentAmount;
    Print();
    return;
  }

  lastUpdate = Date.now();
  updateInterval = setInterval(IntervalUpdate, 10);
}

function IntervalUpdate() {
  let newTime = Date.now();
  let remainingDollars = currentAmount - displayedAmount;

  //Min $0.50 / sec @ $2.00
  //Max $5.00 / sec @ $50.00
  let speed = LerpScale(remainingDollars, 4.00, 50.00, 0.5, 10.00);

  //The change this frame is the Smaller between the calculated speed and the remaining amount
  let delta = Math.min(speed * ((newTime - lastUpdate) / 1000), remainingDollars);

  //Increment displayed amount
  displayedAmount += delta;

  Print();
  lastUpdate = newTime;

  if (displayedAmount >= currentAmount) {
    //Reached target amount
    clearInterval(updateInterval);
  }
}


function SetText() {
  clearInterval(updateInterval);
  Print();
}

function Print() {
  $("#text-DonationDisplay").text(formatter.format(displayedAmount));
}

connection.on('SetState', SetState);
connection.on('UpdateAmount', UpdateAmount);

async function Initiate() {
  await connection.start();
  await connection.invoke('RequestState');
}

function Clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function Lerp(start, end, amt) {
  return (1 - amt) * start + amt * end
}

function InvLerpClamp(value, a, b) {
  return Clamp((value - a) / (b - a), 0.0, 1.0);
}

function LerpScale(value, a, b, start, end) {
  return Lerp(start, end, InvLerpClamp(value, a, b));
}

Initiate();