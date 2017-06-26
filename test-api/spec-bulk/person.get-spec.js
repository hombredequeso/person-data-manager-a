const frisby = require('frisby');
const uuidV4 = require('uuid/v4');

function getRandomInt(min, max) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min)) + min;
}

let testid = uuidV4();


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

const basicSearchBody = 
    {
        "name": {
            "firstName": "",
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
    };

const queryStringTestData = [
    {description: 'search with no query parameters', url: 'http://localhost:8080/api/person/search', responseCode: 200},
    {description: 'search with all valid query parameters',url: 'http://localhost:8080/api/person/search?from=100&size=5', responseCode: 200},
    {description: 'search with invalid "from" query parameter',url: 'http://localhost:8080/api/person/search?from=abc', responseCode: 400},
    {description: 'search with invalid "to" query parameter',url: 'http://localhost:8080/api/person/search?size=abc', responseCode: 400},
];

queryStringTestData.forEach(d => {
    frisby.create(d.description)
        .post(d.url, basicSearchBody, {json: true})
      .expectStatus(d.responseCode)
      .toss();
});


// Paging tests. Each page should return different people.
//
// page 1
var page1Ids = [];
frisby.create('Search page 1')
    .post('http://localhost:8080/api/person/search', basicSearchBody, {json: true})
    .expectStatus(200)
    .afterJSON(function(p1json){
        var page1Ids = p1json.hits.hits.map(x => x._id);

        frisby.create('Search page 2')
            .post('http://localhost:8080/api/person/search?from=10&size=10', basicSearchBody, {json: true})
            .expectStatus(200)
            .expectJSON('hits', 
            {
                'hits': function(a)
                {
                    var p2Ids = a.map(x => x._id); 
                    var allIds = [page1Ids, p2Ids];
                    let intersection = allIds.shift().filter(function(v) {
                        return allIds.every(function(a) {
                            return a.indexOf(v) !== -1;
                        });
                    });
                    expect(intersection.length).toBe(0);
                    // console.log(intersection);
                }
            })
            .toss();
    })
    .toss();


frisby.create('search contains aggregations')
    .post('http://localhost:8080/api/person/search', 
        basicSearchBody
    , {json: true})
    .expectJSON({aggregations: {}})
    .toss();

 
var moreLikeBody = [`"${testid}"`];

frisby.create('POST api/person/morelike')
    .post('http://localhost:8080/api/person/morelike', 
        moreLikeBody
    , {json: true})
  .expectStatus(200)
  .toss();

frisby.create('morelike includes tag aggregation')
    .post('http://localhost:8080/api/person/morelike', 
        moreLikeBody
    , {json: true})
  .expectStatus(200)
  .expectJSON('aggregations', {tagAggs: {}})
  .toss();
