(function () {
    var connection = {};

    function ReceiveNewChats(messageBlock) {
        var scrollBox = $("#chatBox");
        var isScrolledToBottom = scrollBox[0].scrollHeight - scrollBox[0].clientHeight <= scrollBox.scrollTop() + 1;

        messageBlock.messages.forEach((message) => {
            var liElement = document.createElement('li');
            liElement.innerHTML = message.message;
            scrollBox.append(liElement);
        });

        if (isScrolledToBottom) {
            scrollBox.scrollTop(scrollBox[0].scrollHeight);
        }
    }

    function ReceiveNewEvents(messageBlock) {
        var scrollBox = $("#eventBox");
        var isScrolledToBottom = scrollBox[0].scrollHeight - scrollBox[0].clientHeight <= scrollBox.scrollTop() + 1;

        messageBlock.messages.forEach((message) => {
            var liElement = document.createElement('li');
            liElement.innerHTML = message.message;
            scrollBox.append(liElement);
        });

        if (isScrolledToBottom) {
            scrollBox.scrollTop(scrollBox[0].scrollHeight);
        }
    }

    function ReceiveNewDebugs(messageBlock) {
        var scrollBox = $("#debugBox");
        var isScrolledToBottom = scrollBox[0].scrollHeight - scrollBox[0].clientHeight <= scrollBox.scrollTop() + 1;

        messageBlock.messages.forEach((message) => {
            var liElement = document.createElement('li');
            liElement.innerHTML = message.message;
            scrollBox.append(liElement);
        });

        if (isScrolledToBottom) {
            scrollBox.scrollTop(scrollBox[0].scrollHeight);
        }
    }

    function ReceiveNewNotifications(messageBlock) {
        var scrollBox = $("#notificationBox");
        var isScrolledToBottom = scrollBox[0].scrollHeight - scrollBox[0].clientHeight <= scrollBox.scrollTop() + 1;

        messageBlock.messages.forEach((message) => {
            var liElement = $(`<li>${message.message}</li>`);
            let button = $("<button>Replay</button>");
            button.on("click", () => { ReplayNotification(message.id); });
            liElement.prepend(button);
            scrollBox.append(liElement);
        });

        if (isScrolledToBottom) {
            scrollBox.scrollTop(scrollBox[0].scrollHeight);
        }
    }

    function ReceiveNewPendingNotifications(messageBlock) {
        var scrollBox = $("#pendingNotificationBox");
        var isScrolledToBottom = scrollBox[0].scrollHeight - scrollBox[0].clientHeight <= scrollBox.scrollTop() + 1;

        messageBlock.messages.forEach((message) => {
            var liElement = $(`<li id=pending_${message.id}>${message.message}</li>`);

            let denyButton = $("<button>Deny</button>");
            denyButton.on("click", () => { UpdatePendingNotification(message.id, false); });
            liElement.prepend(denyButton);

            let approveButton = $("<button>Approve</button>");
            approveButton.on("click", () => { UpdatePendingNotification(message.id, true); });
            liElement.prepend(approveButton);

            scrollBox.append(liElement);
        });

        if (isScrolledToBottom) {
            scrollBox.scrollTop(scrollBox[0].scrollHeight);
        }
    }

    function NotifyPendingRemoved(index) {
        var element = $("#pending_" + index);
        if (element) {
            element.remove();
        }
    }

    async function AuthCallback(result) {
        if (auth.authString && auth.authString.length > 0 &&
            result.role === "Admin" || result.role === "Privileged") {
            if (connection.invoke("Authenticate", auth.authString)) {
                //Clear Logs
                $("#chatBox").empty();
                $("#eventBox").empty();
                $("#debugBox").empty();
                $("#notificationBox").empty();
                $("#pendingNotificationBox").empty();

                //Request All Data
                ReceiveNewChats(await connection.invoke("RequestAllChats"));
                ReceiveNewEvents(await connection.invoke("RequestAllEvents"));
                ReceiveNewDebugs(await connection.invoke("RequestAllDebugs"));
                ReceiveNewNotifications(await connection.invoke("RequestAllNotifications"));
                ReceiveNewPendingNotifications(await connection.invoke("RequestAllPendingNotifications"));
            }

            if (result.role === "Admin") {
                FetchAudioDevices();
                FetchMidiDevices();
                RefreshSerialDevices();
            }
        }

        RefreshMiscSettings();
        RefreshMicEffect();
        RefreshMicSettings();
    }


    function Authorize() {
        auth.Authorize(AuthCallback);
    }

    function RefreshMiscSettings() {
        $.getJSON({
            url: "/TASagentBotAPI/Settings/ErrorHEnabled",
            success: (result) => {
                $("#input-ErrorH-Enabled").prop('checked', result.enabled);
            }
        });
    }

    function RefreshMicEffect() {
        $.getJSON({
            url: "/TASagentBotAPI/Mic/Effect",
            success: (result) => {
                $("#text-Status").val(result.effect);
            }
        });
    }

    function RefreshMicSettings() {
        $.getJSON({
            url: "/TASagentBotAPI/Mic/Enabled",
            success: (result) => {
                $("#input-Mic-Enabled").prop('checked', result.enabled);
            }
        });

        $.getJSON({
            url: "/TASagentBotAPI/Mic/Compressor",
            success: (result) => {
                $("#input-Compressor-Enabled").prop('checked', result.enabled);
                $("#input-Compressor-Ratio").val(result.ratio);
                $("#input-Compressor-Threshold").val(result.threshold);
                $("#input-Compressor-Attack").val(result.attackDuration);
                $("#input-Compressor-Release").val(result.releaseDuration);
                $("#input-Compressor-Gain").val(result.outputGain);
            }
        });

        $.getJSON({
            url: "/TASagentBotAPI/Mic/NoiseGate",
            success: (result) => {
                $("#input-NoiseGate-Enabled").prop('checked', result.enabled);
                $("#input-NoiseGate-OpenThreshold").val(result.openThreshold);
                $("#input-NoiseGate-CloseThreshold").val(result.closeThreshold);
                $("#input-NoiseGate-Attack").val(result.attackDuration);
                $("#input-NoiseGate-Hold").val(result.holdDuration);
                $("#input-NoiseGate-Release").val(result.releaseDuration);
            }
        });

        $.getJSON({
            url: "/TASagentBotAPI/Mic/Expander",
            success: (result) => {
                $("#input-Expander-Enabled").prop('checked', result.enabled);
                $("#input-Expander-Ratio").val(result.ratio);
                $("#input-Expander-Threshold").val(result.threshold);
                $("#input-Expander-Attack").val(result.attackDuration);
                $("#input-Expander-Release").val(result.releaseDuration);
                $("#input-Expander-Gain").val(result.outputGain);
            }
        });
    }

    function RefreshSerialDevices() {
        $.getJSON({
            url: "/TASagentBotAPI/ControllerSpy/GetPorts",
            headers: { "Authorization": "PASS NONE" },
            success: (devices) => {
                var serialDeviceSelect = $("#select-SerialDevice");

                serialDeviceSelect.empty();

                serialDeviceSelect.append($("<option value=\"\">None</option>"))

                devices.forEach((device) => {
                    serialDeviceSelect.append($(`<option value="${device}">${device}</option>`));
                });

                UpdateCurrentSerialDevice();
            },
            beforeSend: auth.ApplyAuth,
            error: (response) => {
                if (response.status && response.status === 401) {
                    auth.SetAuthStatus("None");
                } else if (response.status && response.status === 404) {
                    //No input capture module exists - Delete
                    $("#nav-settings-inputcapture-tab").remove();
                    $("#nav-settings-inputcapture").remove();
                }
            }
        });
    }


    function UpdateCurrentSerialDevice() {
        $.getJSON({
            url: "/TASagentBotAPI/ControllerSpy/CurrentPort",
            headers: { "Authorization": "PASS NONE" },
            success: (device) => { $("#select-SerialDevice").val(device); },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitSerialDeviceChanged(port) {
        PrintText(`Serial Device being changed to: ${port}`);
        $.post({
            url: "/TASagentBotAPI/ControllerSpy/Attach",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Port: port }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function RequestTimerLayoutDisplayModes() {
        $.getJSON({
            url: "/TASagentBotAPI/Timer/DisplayModes",
            success: (result) => {
                var timerMainValueSelect = $("#select-TimerMainValue");
                var timerSecondaryValueSelect = $("#select-TimerSecondaryValue");

                timerMainValueSelect.empty();
                timerSecondaryValueSelect.empty();

                result.forEach((timerLayoutValue) => {
                    timerMainValueSelect.append($(`<option value="${timerLayoutValue.value}">${timerLayoutValue.display}</option>`));
                    timerSecondaryValueSelect.append($(`<option value="${timerLayoutValue.value}">${timerLayoutValue.display}</option>`));
                });
            },
            error: (response) => {
                if (response.status && response.status === 404) {
                    //Timer probably not enabled - just delete it.
                    $("#nav-settings-timer-tab").remove();
                    $("#nav-settings-timer").remove();
                    $("#nav-tools-timer-tab").remove();
                    $("#nav-tools-timer").remove();
                }
            }
        });
    }

    function SubmitMicEnabled(enabled) {
        $.post({
            url: "/TASagentBotAPI/Mic/Enabled",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Enabled: enabled }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
        setTimeout(RefreshMicSettings, 200);
    }

    function SubmitErrorHEnabled(enabled) {
        $.post({
            url: "/TASagentBotAPI/Settings/ErrorHEnabled",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Enabled: enabled }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
        setTimeout(RefreshMiscSettings, 200);
    }

    function SubmitEffect(effect) {
        $.post({
            url: "/TASagentBotAPI/Mic/Effect",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Effect: effect }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
        setTimeout(RefreshMicEffect, 200);
    }

    function SubmitNoiseGate() {
        $.post({
            url: "/TASagentBotAPI/Mic/NoiseGate",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                Enabled: $("#input-NoiseGate-Enabled").prop('checked'),
                OpenThreshold: $("#input-NoiseGate-OpenThreshold").val(),
                CloseThreshold: $("#input-NoiseGate-CloseThreshold").val(),
                AttackDuration: $("#input-NoiseGate-Attack").val(),
                HoldDuration: $("#input-NoiseGate-Hold").val(),
                ReleaseDuration: $("#input-NoiseGate-Release").val()
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitCompressor() {
        $.post({
            url: "/TASagentBotAPI/Mic/Compressor",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                Enabled: $("#input-Compressor-Enabled").prop('checked'),
                Ratio: $("#input-Compressor-Ratio").val(),
                Threshold: $("#input-Compressor-Threshold").val(),
                AttackDuration: $("#input-Compressor-Attack").val(),
                ReleaseDuration: $("#input-Compressor-Release").val(),
                OutputGain: $("#input-Compressor-Gain").val()
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitExpander() {
        $.post({
            url: "/TASagentBotAPI/Mic/Expander",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                Enabled: $("#input-Expander-Enabled").prop('checked'),
                Ratio: $("#input-Expander-Ratio").val(),
                Threshold: $("#input-Expander-Threshold").val(),
                AttackDuration: $("#input-Expander-Attack").val(),
                ReleaseDuration: $("#input-Expander-Release").val(),
                OutputGain: $("#input-Expander-Gain").val()
            }),
            beforeSend: ApplyAuth,
            error: HandleErrorResponse
        });
    }

    function SubmitTTS() {
        $.post({
            url: "/TASagentBotAPI/TTS/Play",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                voice: $("#select-TTSVoice").val(),
                pitch: $("#select-TTSPitch").val(),
                speed: $("#select-TTSSpeed").val(),
                effect: $("#input-TTSEffect").val(),
                text: $("#input-TTSText").val(),
                user: $("#input-TTSUser").val()
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });

        //Clear TTS input contingent on checkbox
        if ($("#input-ClearTTSOnSend").prop('checked')) {
            $("#input-TTSText").val("");
        }
    }

    function UpdateTTSSettings() {
        $.get({
            url: "/TASagentBotAPI/TTS/Settings",
            headers: { "Authorization": "PASS NONE" },
            success: (settings) => {
                $("#input-TTS-Enabled").prop('checked', settings.enabled);
                $("#input-TTS-BitThreshold").val(settings.bitThreshold);
                $("#input-TTS-Command-Enabled").prop('checked', settings.commandEnabled);
                $("#input-TTS-Command-Cooldown").val(settings.commandCooldown);
                $("#input-TTS-Redemption-Enabled").prop('checked', settings.redemptionEnabled);
                $("#input-TTS-Neural-Enabled").prop('checked', settings.allowNeuralVoices);
            },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitTTSSettings() {
        $.post({
            url: "/TASagentBotAPI/TTS/Settings",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                enabled: $("#input-TTS-Enabled").prop('checked'),
                bitThreshold: $("#input-TTS-BitThreshold").val(),
                commandEnabled: $("#input-TTS-Command-Enabled").prop('checked'),
                commandCooldown: $("#input-TTS-Command-Cooldown").val(),
                redemptionEnabled: $("#input-TTS-Redemption-Enabled").prop('checked'),
                allowNeuralVoices: $("#input-TTS-Neural-Enabled").prop('checked')
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function MoveSlider() {
        var value = $('#slider-PitchSlider').slider("value");
        var pitchValue = Math.pow(8, value);

        $.post({
            url: "/TASagentBotAPI/Mic/PitchFactor",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Factor: pitchValue }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SkipNotification() {
        $.post({
            url: "/TASagentBotAPI/Notifications/SkipNotification",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function Quit() {
        $.post({
            url: "/TASagentBotAPI/Event/Quit",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function PrintText(message) {
        $.post({
            url: "/TASagentBotAPI/Event/Print",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Message: message }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SpeakText(message) {
        $.post({
            url: "/TASagentBotAPI/Event/Speak",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Message: message }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function PlaySound(sound) {
        $.post({
            url: "/TASagentBotAPI/SFX/PlayImmediate",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Effect: sound }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function UpdatePendingNotification(index, approved) {
        $.post({
            url: "/TASagentBotAPI/Notifications/UpdatePendingNotification",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                Index: index,
                Approved: approved
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function ReplayNotification(index) {
        $.post({
            url: "/TASagentBotAPI/Notifications/ReplayNotification",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Index: index }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitEffectOutputDevice(device) {
        PrintText(`Effect output device being changed to: ${device}`);
        $.post({
            url: "/TASagentBotAPI/Settings/CurrentEffectOutputDevice",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Device: device }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitVoiceOutputDevice(device) {
        PrintText(`Voice output device being changed to: ${device}`);
        $.post({
            url: "/TASagentBotAPI/Settings/CurrentVoiceOutputDevice",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Device: device }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitVoiceInputDevice(device) {
        PrintText(`Voice input device being changed to: ${device}`);
        $.post({
            url: "/TASagentBotAPI/Settings/CurrentVoiceInputDevice",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Device: device }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitMidiOutputDevice(device) {
        PrintText(`Midi output device being changed to: ${device}`);
        $.post({
            url: "/TASagentBotAPI/Midi/CurrentMidiOutputDevice",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Device: device }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitMidiDevice(device) {
        PrintText(`Midi device being changed to: ${device}`);
        $.post({
            url: "/TASagentBotAPI/Midi/CurrentMidiDevice",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({ Device: device }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitMidiInstrument(value) {
        PrintText(`Midi instrument being changed to: ${value}`);
        $.post({
            url: "/TASagentBotAPI/Midi/CurrentMidiInstrument",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                Effect: value,
                Channel: 1
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitMidiSoundEffect(value) {
        PrintText(`Midi Sound Effect being changed to: ${value}`);
        $.post({
            url: "/TASagentBotAPI/Midi/CurrentMidiSoundEffect",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                Effect: value,
                Channel: 1
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function FetchAudioDevices() {
        $.get({
            url: "/TASagentBotAPI/Settings/OutputDevices",
            headers: { "Authorization": "PASS NONE" },
            success: (devices) => {
                var effectOutputSelect = $("#select-EffectOutputDevice");
                var voiceOutputSelect = $("#select-VoiceOutputDevice");
                var midiOutputSelect = $("#select-MidiOutputDevice");

                effectOutputSelect.empty();
                voiceOutputSelect.empty();
                midiOutputSelect.empty();

                devices.forEach((device) => {
                    var effectOptionElement = $(`<option value="${device}">${device}</option>`);
                    effectOutputSelect.append(effectOptionElement);

                    var voiceOptionElement = $(`<option value="${device}">${device}</option>`);
                    voiceOutputSelect.append(voiceOptionElement);

                    var midiOptionElement = $(`<option value="${device}">${device}</option>`);
                    midiOutputSelect.append(midiOptionElement);
                });

                UpdateCurrentOutputDevices();
            },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });

        $.get({
            url: "/TASagentBotAPI/Settings/InputDevices",
            headers: { "Authorization": "PASS NONE" },
            success: (devices) => {
                var voiceInputSelect = $("#select-VoiceInputDevice");

                voiceInputSelect.empty();

                devices.forEach((device) => {
                    var optionElement = $(`<option value="${device}">${device}</option>`);
                    voiceInputSelect.append(optionElement);
                });

                UpdateCurrentInputDevice();
            },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function FetchMidiDevices() {
        $.get({
            url: "/TASagentBotAPI/Midi/MidiDevices",
            headers: { "Authorization": "PASS NONE" },
            success: (devices) => {
                var midiDeviceSelect = $("#select-MidiDevice");

                midiDeviceSelect.empty();
                var noneOptionElement = $(`<option value="">None</option>`);
                midiDeviceSelect.append(noneOptionElement);

                devices.forEach((device) => {
                    var optionElement = $(`<option value="${device}">${device}</option>`);
                    midiDeviceSelect.append(optionElement);
                });

                UpdateCurrentMidiDevice();
            },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });

        $.get({
            url: "/TASagentBotAPI/Midi/MidiInstruments",
            headers: { "Authorization": "PASS NONE" },
            success: (devices) => {
                var midiInstrumentSelect = $("#select-MidiInstrument");

                midiInstrumentSelect.empty();

                devices.forEach((device) => {
                    var optionElement = $(`<option value="${device}">${device}</option>`);
                    midiInstrumentSelect.append(optionElement);
                });
            },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function UpdateCurrentOutputDevices() {
        $.get({
            url: "/TASagentBotAPI/Settings/CurrentEffectOutputDevice",
            headers: { "Authorization": "PASS NONE" },
            success: (device) => { $("#select-EffectOutputDevice").val(device); },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });

        $.get({
            url: "/TASagentBotAPI/Settings/CurrentVoiceOutputDevice",
            headers: { "Authorization": "PASS NONE" },
            success: (device) => { $("#select-VoiceOutputDevice").val(device); },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });

        $.get({
            url: "/TASagentBotAPI/Midi/CurrentMidiOutputDevice",
            headers: { "Authorization": "PASS NONE" },
            success: (device) => { $("#select-MidiOutputDevice").val(device); },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function UpdateCurrentInputDevice() {
        $.get({
            url: "/TASagentBotAPI/Settings/CurrentVoiceInputDevice",
            headers: { "Authorization": "PASS NONE" },
            success: (device) => { $("#select-VoiceInputDevice").val(device); },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function UpdateCurrentMidiDevice() {
        $.get({
            url: "/TASagentBotAPI/Midi/CurrentMidiDevice",
            headers: { "Authorization": "PASS NONE" },
            success: (device) => { $("#select-MidiDevice").val(device); },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitTimerTime(value) {
        PrintText(`Timer being changed to: ${value}`);
        $.post({
            url: "/TASagentBotAPI/Timer/Set",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                Time: parseFloat(value)
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function TriggerTimerAction(action) {
        $.post({
            url: `/TASagentBotAPI/Timer/${action}`,
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function FetchTimerValues() {
        $.get({
            url: "/TASagentBotAPI/Timer/TimerState",
            headers: { "Authorization": "PASS NONE" },
            success: (timerState) => {
                var lapStartTime = 0;
                timerState.laps.forEach((value) => { lapStartTime += value; });

                $("#input-TimerCumulative").val(timeToString(lapStartTime + timerState.currentMS));
                $("#input-TimerCurrent").val(timeToString(timerState.currentMS));
                $("#input-TimerLap").val(timeToString(lapStartTime));
                $("#input-TimerLapCount").val(timerState.laps.length);

                $("#input-TimerMainLabel").val(timerState.layout.mainLabel);
                $("#select-TimerMainValue").val("" + timerState.layout.mainDisplay);
                $("#input-TimerSecondaryLabel").val(timerState.layout.secondaryLabel);
                $("#select-TimerSecondaryValue").val("" + timerState.layout.secondaryDisplay);
            },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function FetchSavedTimerValues() {
        $.get({
            url: "/TASagentBotAPI/Timer/SavedTimers",
            headers: { "Authorization": "PASS NONE" },
            success: (timers) => {
                var timerLoadSelect = $("#select-TimerLoad");

                timerLoadSelect.empty();

                timers.forEach((timer) => {
                    var cumulativeTime = timer.endingTime;
                    timer.laps.forEach((value) => { cumulativeTime += value; });

                    var timerOptionElement = $(`<option value="${timer.name}">${timer.name}: ${timeToString(cumulativeTime)}</option>`);
                    timerLoadSelect.append(timerOptionElement);
                });

                $("#button-TimerLoad").prop("disabled", false);
            },
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SubmitTimerLayout() {
        $.post({
            url: "/TASagentBotAPI/Timer/DisplayMode",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                MainLabel: $("#input-TimerMainLabel").val(),
                MainDisplay: parseInt($("#select-TimerMainValue").val()),
                SecondaryLabel: $("#input-TimerSecondaryLabel").val(),
                SecondaryDisplay: parseInt($("#select-TimerSecondaryValue").val())
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function LoadTimer(timer) {
        PrintText(`Loading Timer: ${timer}`);
        $.post({
            url: "/TASagentBotAPI/Timer/LoadTimer",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                TimerName: timer
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    function SaveTimer(timer) {
        PrintText(`Saving Timer as: ${timer}`);
        $.post({
            url: "/TASagentBotAPI/Timer/SaveTimer",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify({
                TimerName: timer
            }),
            beforeSend: auth.ApplyAuth,
            error: auth.HandleErrorResponse
        });
    }

    $(document).ready(() => {

        auth.SetAuthStatus("None");

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/Hubs/Monitor")
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveNewChats', ReceiveNewChats);
        connection.on('ReceiveNewDebugs', ReceiveNewDebugs);
        connection.on('ReceiveNewEvents', ReceiveNewEvents);
        connection.on('ReceiveNewNotifications', ReceiveNewNotifications);
        connection.on('ReceiveNewPendingNotifications', ReceiveNewPendingNotifications);
        connection.on('NotifyPendingRemoved', NotifyPendingRemoved);

        connection.start();

        $("#button-Submit-Mic-Enabled").click(function () {
            SubmitMicEnabled($("#input-Mic-Enabled").prop('checked'));
        });

        $("#button-Submit-errorh-Enabled").click(function () {
            SubmitErrorHEnabled($("#input-ErrorH-Enabled").prop('checked'));
        });

        $("#button-Normal").click(function () {
            SubmitEffect("");
        });

        $("#button-HighPitch").click(function () {
            SubmitEffect("PitchShift 1.5000");
        });

        $("#button-LowPitch").click(function () {
            SubmitEffect("PitchShift 0.75000");
        });

        $("#button-Modulation").click(function () {
            SubmitEffect("Modulate 4 200");
        });

        $("#button-Vocoding").click(function () {
            SubmitEffect("Vocode 15");
        });

        $("#button-VocodingPitch").click(function () {
            SubmitEffect("Vocode 15, Pitch 1.5");
        });

        $("#button-Chorus").click(function () {
            SubmitEffect("Chorus 40 60 0.25 Sine");
        });

        $("#button-ReverbVeryWarm").click(function () {
            SubmitEffect("Reverb VeryWarm");
        });

        $("#button-ReverbInsideEar").click(function () {
            SubmitEffect("Reverb InsideEar");
        });

        $("#button-ReverbInsideBox").click(function () {
            SubmitEffect("Reverb InsideBox");
        });

        $("#button-WorkingRobot").click(function () {
            SubmitEffect("Reverb crappyspeaker, FreqShift 200");
        });

        $("#button-BrokenRobot").click(function () {
            SubmitEffect("FreqShift 800, Reverb Crappyspeaker, FreqShift -400");
        });

        $("#button-Strange").click(function () {
            SubmitEffect("PitchShift 0.75, FreqShift 400, Reverb Crappyspeaker");
        });

        $("#button-Orb").click(function () {
            SubmitEffect("Echo 200 0.3, PitchShift 0.75");
        });

        $("#button-OrbHigh").click(function () {
            SubmitEffect("Echo 200 0.3, PitchShift 1.75");
        });

        $("#button-Echo").click(function () {
            SubmitEffect("Echo 300 0.4");
        });

        $("#button-SubmitCustomEffect").click(function () {
            SubmitEffect($("#input-CustomEffect").val());
        });

        $("#slider-PitchSlider").slider({
            value: 0,
            min: -1,
            max: 1,
            step: 0.01,
            slide: MoveSlider
        })

        $("#button-Submit").click(Authorize);
        $("#button-SubmitTTS").click(SubmitTTS);

        $("#button-SubmitNoiseGateSettings").click(SubmitNoiseGate);
        $("#button-SubmitCompressorSettings").click(SubmitCompressor);
        $("#button-SubmitExpanderSettings").click(SubmitExpander);

        $("#button-Quit").click(Quit);

        $("#button-Skip").click(SkipNotification);

        $("#button-PlaySound").click(function () {
            PlaySound($("#input-PlaySound").val());
            $("#input-PlaySound").val("");
        });

        $("#button-SendMessage").click(function () {
            PrintText($("#input-SendMessage").val());
            $("#input-SendMessage").val("");
        });

        $("#button-Speak").click(function () {
            SpeakText($("#input-Speak").val());
            $("#input-Speak").val("");
        });

        var effectOutputDevice = $("#select-EffectOutputDevice");
        effectOutputDevice.change(function () { SubmitEffectOutputDevice(effectOutputDevice.val()); });

        var voiceOutputDevice = $("#select-VoiceOutputDevice");
        voiceOutputDevice.change(function () { SubmitVoiceOutputDevice(voiceOutputDevice.val()); });

        var voiceInputDevice = $("#select-VoiceInputDevice");
        voiceInputDevice.change(function () { SubmitVoiceInputDevice(voiceInputDevice.val()); });

        var midiOutputDevice = $("#select-MidiOutputDevice");
        midiOutputDevice.change(function () { SubmitMidiOutputDevice(midiOutputDevice.val()); });

        var midiDevice = $("#select-MidiDevice");
        midiDevice.change(function () { SubmitMidiDevice(midiDevice.val()); });


        $("#button-MidiBindSFX").click(function () {
            SubmitMidiSoundEffect($("#input-MidiBindSFX").val());
        });

        $("#button-MidiBindInstrument").click(function () {
            SubmitMidiInstrument($("#select-MidiInstrument").val());
        });

        $("#button-TimerSetTime").click(function () {
            SubmitTimerTime($("#input-TimerSetTime").val());
        });

        $("#button-TimerStart").click(function () {
            TriggerTimerAction("Start");
        });

        $("#button-TimerStop").click(function () {
            TriggerTimerAction("Stop");
        });

        $("#button-TimerReset").click(function () {
            TriggerTimerAction("Reset");
        });

        $("#button-TimerLap").click(function () {
            TriggerTimerAction("MarkLap");
        });

        $("#button-TimerUnlap").click(function () {
            TriggerTimerAction("UnmarkLap");
        });

        $("#button-TimerResetLap").click(function () {
            TriggerTimerAction("ResetCurrentLap");
        });

        $("#button-TimerLoad").click(function () {
            LoadTimer($("#select-TimerLoad").val());
        });

        $("#button-TimerSave").click(function () {
            SaveTimer($("#input-TimerSaveName").val());
        });

        $("#button-TimerFetchSaved").click(FetchSavedTimerValues);
        $("#button-TimerApplyLayout").click(SubmitTimerLayout);

        $("#button-RefreshMicEffect").click(RefreshMicEffect);

        $("#button-Submit-TTS-Settings").click(SubmitTTSSettings);

        var serialDeviceSelect = $("#select-SerialDevice");
        serialDeviceSelect.change(function () { SubmitSerialDeviceChanged(serialDeviceSelect.val()); });

        //Trigger refresh on tab change
        //Tools tabs
        $("#nav-tools-miceffect-tab").click(RefreshMicEffect);
        $("#nav-tools-timer-tab").click(FetchTimerValues).click(FetchSavedTimerValues);
        //Settings tabs
        $("#nav-settings-misc-tab").click(RefreshMiscSettings);
        $("#nav-settings-mic-tab").click(RefreshMicSettings);
        $("#nav-settings-inputcapture-tab").click(RefreshSerialDevices);
        $("#nav-settings-audio-tab").click(FetchAudioDevices);
        $("#nav-settings-midi-tab").click(FetchMidiDevices);
        $("#nav-settings-timer-tab").click(FetchTimerValues);
        $("#nav-settings-tts-tab").click(UpdateTTSSettings);


        RefreshMiscSettings();
        RefreshMicEffect();
        RefreshMicSettings();
        RequestTimerLayoutDisplayModes();
    });
})();