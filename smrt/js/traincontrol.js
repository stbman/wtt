// Train control file

var trainsDeployed = [];

// Dummy object 
var trainInMotion = {
    "id": 0,
    "stationNow": [],
    "stationNext": [],
    "travelDelta": 0.5,
    "breakdown": false
}

// TODO: Add levels? 
var numOfTrains = 10;

function createRandomTrains() {
    for (var i = 0; i < numOfTrains; i++) {
        var randomTrain = {
            "id": i,
            "breakdown": false
        };

        randomTrain["travelDelta"] = 0;

        randomLine = generateRandomLine();
        randomStationIdx = generateRandomNumber(0, randomLine.length);

        randomTrain["stationNow"] = randomLine[randomStationIdx];
        randomTrain["stationNext"] = randomLine[randomStationIdx + 1];

        trainsDeployed.push(randomTrain);
       
    }
}