var timer = 1000000;
var streamTimer = 10000;
var connectionUrl = document.getElementById("connectionUrl")
var connectButton = document.getElementById("connectButton")
var stateLabel = document.getElementById("stateLabel")
var streamButton = document.getElementById("stream")
var commsLog = document.getElementById("commsLog")
var closeButton = document.getElementById("closeButton")
var recipents = document.getElementById("recipents");
var connID = document.getElementById("connIDLabel")
var video = document.getElementById("video");
var client = document.getElementById("client");
var videoTrack;
var captureStream;
var recordedChunks = [];
var blobArray = [];
var receiveBase64;

connectionUrl.value = "ws://localhost:5000";

connectButton.onclick = function () {
    stateLabel.innerHTML = "Attempting to connect...";
    ws = new WebSocket(connectionUrl.value)
    ws.onopen = function (event) {
        updateState();
        commsLog.innerHTML += '<tr>' +
            '<td colspan="3"> Connection opened</td>' +
            '</tr>';
    };

    ws.onclose = function (event) {
        updateState();
    };

    ws.onerror = updateState();
    ws.onmessage = function (e) {
        var data = JSON.parse(e.data);
        let ID = undefined;

        updateCurrentUsers(data);

        if ('base64String' in data) {
            download(data)
        }

        try {
            ID = data.ID;
            checkID(ID);
            isConnID(data.ID);
        } catch (error) {
            console.log(error);

        }
    }
    /*
    ws.onmessage = function (event) {
        const obj = JSON.parse(event.data);

        updateCurrentUsers(obj);

        if (obj.hasOwnProperty("usersAmount") && !obj.hasOwnProperty("ID")) {
            return -1;
        }
        if (obj.hasOwnProperty("Base64obj")) {
            streamVideo(obj);
        }

        isConnID(obj.ID);


    };
    */
};

function streamVideo(stream) {
    var options = { mimeType: "video/webm; codecs=vp9" };
    mediaRecorder = new MediaRecorder(stream, options);

    mediaRecorder.ondataavailable = handleDataAvailable;
    mediaRecorder.start();
}


function handleDataAvailable(event) {
    if (event.data.size > 0) {
        blobToByteArray(event.data);
    }
}
function blobToByteArray(blob) {
    var arrayPromise = new Promise(function (resolve) {
        var reader = new FileReader();

        reader.onloadend = function () {
            resolve(reader.result);
        };

        reader.readAsArrayBuffer(blob);
    });

    arrayPromise.then(function (array) {
        recordedChunks.push(array);
        sendStream(recordedChunks[0]);
    });


}

function download(data) {
    var client = document.getElementById('client');
    var source = document.createElement('source');


    var buffer = _base64ToArrayBuffer(data.base64String);
    try {
        blobArray.push(new Blob([new Uint8Array(buffer)], { 'type': 'video/mp4' }));
        let blob = new Blob(blobArray, { 'type': 'video/mp4' });
        let currentTime = client.currentTime;

        client.src = window.URL.createObjectURL(blob);
        client.currentTime = currentTime;

    }
    catch (err) {
        console.log(err);
    }
}

function _base64ToArrayBuffer(base64) {
    var binary_string = window.atob(base64);
    var len = binary_string.length;
    var bytes = new Uint8Array(len);
    for (var i = 0; i < len; i++) {
        bytes[i] = binary_string.charCodeAt(i);
    }
    return bytes.buffer;
}

function updateCurrentUsers(obj) {
    document.getElementById("currentlyConnected").innerHTML = `Connected Users: ${obj.usersAmount}`;
}

function getAmountOfUsers() {
    var data = constructJSON(1, null);
    try {
        ws.send(data);
        const obj = JSON.parse(data);
    }
    catch (err) {
        document.getElementById("currentlyConnected").innerHTML = `Connected Users: User not found`;
    }
}

var refreshIntervalId = setInterval(getAmountOfUsers, timer);

function sendStream(array) {

    var i, j, temporary, chunk = 1024 * 4;

    for (i = 0, j = array.byteLength; i < j; i += chunk) {
        temporary = array.slice(i, i + chunk);
        var base64String = btoa(String.fromCharCode.apply(null, new Uint8Array(temporary)));


        const obj = {
            operation: 0,
            From: connID.innerHTML.substring(8, connID.innerHTML.length),
            To: recipents.value,
            base64String: base64String
        }

        const json = JSON.stringify(obj);
        sendMessage(ws, json);
    }
}


closeButton.onclick = function () {
    if (!ws || ws.readyState !== ws.OPEN) {
        alert("Socket not connected");
    }
    ws.close(1000, "Closing from client");
};

streamButton.onclick = async function startCapture(displayMediaOptions) {

    try {
        video.srcObject = await navigator.mediaDevices.getDisplayMedia(displayMediaOptions)
    } catch (err) {
        console.error("Error: " + err);
    }

    videoTrack = video.srcObject.getTracks()[0];

    var stream = await video.captureStream(30);
    streamVideo(stream);
};

function checkID(ID) {

    if (localStorage.getItem("ID") === null) {
        storeID(ID);
    }
    else {
        isConnID(ID);
        return -1;
    }

}

function storeID(ID) {

    const obj = {
        ID: ID,
    };
    localStorage.setItem("ID", JSON.stringify(obj));
}




function isConnID(str) {
    if (str.length > 0) {
        connID.innerHTML = "ConnID: " + str;
    }
};

function constructJSON(operation, base64) {

    if (operation === 0) {
        return JSON.stringify({
            "operation": operation,
            "From": connID.innerHTML.substring(8, connID.innerHTML.length),
            "To": recipents.value,
            "base64String": base64
        });
    }
    else if (operation == 1) {
        return JSON.stringify({
            "operation": operation
        });
    }


}


function updateState() {
    function disable() {
        streamButton.disabled = true;
        closeButton.disabled = true;
        recipents.disabled = true;
    };
    function enable() {
        streamButton.disabled = false;
        closeButton.disabled = false;
        recipents.disabled = false;
    };
    connectionUrl.disabled = true;
    if (!ws) {
        disable();
    } else {
        switch (ws.readyState) {
            case ws.CLOSED:
                stateLabel.innerHTML = "Closed";
                connID.innerHTML = "ConnID: N/a";
                disable();
                connectionUrl.disable = false;
                clearInterval(refreshIntervalId);
                break;
            case ws.CLOSING:
                stateLabel.innerHTML = "Closing...";
                disable();
                break;
            case ws.OPEN:
                stateLabel.innerHTML = "Open";
                enable();
                break;
            default:
                stateLabel.innerHTML = "Unknown WebSocket State: "
                disable();
                break;
        }
    }
};