const faker = require('faker');
const randomCoord = require('geo-utils/lib/random-geo-coord')
const weightedRandom = require('geo-utils/lib/weighted-random')
const uuidV4 = require('uuid/v4');
const randomSubset = require("random-array-subset")

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
// const tagList = Array(tagCount).fill(0).map((x,i) => `item${i}`);
const tagList = ['javascript', 'c#', 'haskell', 'sql', 'elasticsearch', 'neo4j', 'agile', 'discovery', 'distributed systems', 'monolith messes'];

const poolDescriptions = [null, 'Junior Developer', 'Senior Developer', 'Architect'];
let pools = poolDescriptions.map((x) => {return {id: uuidV4(), description: x};});
const statuses = ['Stage1', 'Stage2', 'Stage3'];

function* personGenerator() {

    while (true) {

        let randomLocation = weightedRandom.get('population', locations);
        let geoCoord = randomCoord.randomizedCoord(Math.random, randomLocation.location, 30, 1.5);
        let firstName = faker.name.firstName();
        let lastName = faker.name.lastName();
        let email = faker.internet.email(firstName, lastName);
        let phoneNumber = faker.phone.phoneNumber();

        let poolStatuses = [];
        let pool = faker.random.arrayElement(pools);
        if (pool.description !== null) {
            let status = faker.random.arrayElement(statuses);
            poolStatuses.push({pool: pool, status: status});
        }

        yield {
            "id": uuidV4(),
            "name": {
                firstName: firstName,
                lastName: lastName
            },
            "tags": randomSubset(tagList, faker.random.number({min:2, max:5}) ),
            "poolStatuses": poolStatuses,
            "contact": {
                "phone": [
                    {"label": "home", "number": phoneNumber}
                ],
                "email": [
                    {"label": "work", "address": email}
                ]
            },
            "address": {
                "region": randomLocation.city,
                "geo": {
                    "coord": geoCoord
                }
            }
        };
    }
};

module.exports = personGenerator;
