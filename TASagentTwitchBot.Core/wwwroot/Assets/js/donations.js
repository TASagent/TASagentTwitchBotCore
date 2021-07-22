// Convert time to a format of hours, minutes, seconds, and milliseconds

let connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/Donation")
    .build();

let formatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD'
});

function SetAmount(stateValue) {
    $("#text-DonationDisplay").text(formatter.format(stateValue.currentAmount));
}

connection.on('SetAmount', SetAmount);

async function Initiate() {
    await connection.start();
    await connection.invoke("RequestAmount");
}

Initiate();