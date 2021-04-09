
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/Overlay")
    .build();

connection.on('ReceiveImageNotification',
    function (text, duration, image) {
        $("#divText").html(text).hide().fadeIn(250).delay(duration).fadeOut(250);
        $("#divImage").html(image).hide().fadeIn(250).delay(duration).fadeOut(250);
    });

connection.on('ReceiveVideoNotification',
    function (text, duration, video) {
        $("#divText").html(text).hide().delay(100).fadeIn(100).delay(duration - 400).fadeOut(100);
        $("#divImage").html(video).hide().fadeIn(10).delay(duration).fadeOut(10);
    });

connection.start();