var frisby = require('frisby');

function getRandomInt(min, max) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min)) + min;
}


let testid = "f0a6261b-6c4a-4af4-b92b-d3c51177f41f";

frisby.create('POST api/person')
    .post('http://localhost:8080/api/person', {
        "id": testid,
        "name" : {
            "firstName": "bob",
            "lastName": "mcbob"
        },
        "contact": {
            "phone": [
              {
                "label": "home",
                "number": "07 9876 5432"
              }
            ],
            "email": [
              {
                "label": "work",
                "address": "bob@mcbob.com"
              }
            ]
        },
        "tags" : ["item1", "item2", "item3"],
        "poolStatuses": [],
        "address": {
            "geo": {
              "coord": {
                "lat": -37.80710456081047,
                "lon": 144.96544319139053
              }
            }
            }
        }, {json: true})
  .expectStatus(201)
  .toss();

frisby.create(`GET api/person/${testid}`)
  .get(`http://localhost:8080/api/person/${testid}`)
  .expectStatus(200)
  .expectHeaderContains('content-type', 'application/json')
  .toss();

frisby.create('POST api/person/search')
    .post('http://localhost:8080/api/person/search', {
        "name": {
            "firstName": "bob",
            "lastName": ""
        },
        "tags": ["item3"],
        "near": {
            "coord": {
                "lat": -37.814,
                "lon": 144.963
            },
            "distance" : 40
        }
    }, {json: true})
  .expectStatus(200)
  .toss();

var moreLikeBody = [`"${testid}"`];

frisby.create('POST api/person/morelike')
    .post('http://localhost:8080/api/person/morelike', 
        moreLikeBody
    , {json: true})
  .expectStatus(200)
  .toss();

