const faker = require('faker');
const randomCoord = require('geo-utils/lib/random-geo-coord')
const weightedRandom = require('geo-utils/lib/weighted-random')
const randomSubset = require('../lib/random-subset');

faker.locale = "en_AU";

let locations = 
[
    {
        "city": "Sydney",
        "location": {
            "lat": -33.868,
            "lon": 151.207
        },
        "population": 4627345
    },
    {
        "city": "Brisbane",
        "location": {
            "lat": -27.468,
            "lon": 153.028
        },
        "population": 2189878
    },
    {
        "city": "Perth",
        "location": {
            "lat": -31.952,
            "lon": 115.861
        },
        "population": 1896548
    },
    {
        "city": " Adelaide",
        "location": {
            "lat": -34.929,
            "lon": 138.599
        },
        "population": 1225235
    },
    {
        "city": "Gold Coast",
        "location": {
            "lat": -28,
            "lon": 153.431
        },
        "population": 591473
    },
    {
        "city": "Canberra",
        "location": {
            "lat": -35.283,
            "lon": 149.128
        },
        "population": 367752
    },
    {
        "city": "Newcastle",
        "location": {
            "lat": -32.927,
            "lon": 151.776
        },
        "population": 308308
    },
    {
        "city": "Wollongong",
        "location": {
            "lat": -34.424,
            "lon": 150.893
        },
        "population": 292190
    },
    {
        "city": "Logan City",
        "location": {
            "lat": -27.639,
            "lon": 153.109
        },
        "population": 282673
    },
    {
        "city": "Melbourne",
        "location": {
            "lat": -37.814,
            "lon": 144.963
        },
        "population": 4246375
    }
];


const tagCount = 10;
const tagList = Array(tagCount).fill(0).map((x,i) => `item${i}`);

function* personGenerator() {
    let id = 1;

    while (true) {

        let geoCoord = randomCoord.randomizedCoord(Math.random, weightedRandom.get('population', locations).location, 30, 1.5);

        yield {
            "id": id++,
            "name": faker.name.findName(),
            "email": faker.internet.email(),
            "tags": randomSubset.getNDistinctFrom(
                                    faker.random.arrayElement, 
                                    tagList,
                                    faker.random.number({min:2, max:5})),
            "poolStatuses": [],
            "geo": {
                "coord": geoCoord
            }
        };
    }
};

module.exports = personGenerator;
