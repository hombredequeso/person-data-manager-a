var frisby = require('frisby');

function getRandomInt(min, max) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min)) + min;
}


let testid = getRandomInt(100000, 900000);

frisby.create('POST api/person')
    .post('http://localhost:8080/api/person', {
        "id": testid,
        "name" : "bob mcbob",
        "email" : "bob@gmail.com",
        "tags" : ["item1", "item2", "item3"],
        "poolStatuses": [],
        "geo": {
          "coord": {
            "lat": -37.80710456081047,
            "lon": 144.96544319139053
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
        "name": "bob",
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

