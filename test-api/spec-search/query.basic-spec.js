const frisby = require('frisby');
const uuidV4 = require('uuid/v4');

const Person = require('./person-builder').Person;
const Address = require('./person-builder').Address;

class QueryBody {
    constructor(id) {
        this.queryParameters = {
            tags: []
        };
        this.metadata = {
            id: id
        }
    };
}

const queryId = uuidV4();
let newQuery = new QueryBody(queryId);

frisby.create('POST new query')
    .post('http://localhost:8080/api/query', newQuery, {json: true})
    .expectStatus(201)
    .toss();


// GET query that doesn't exist
let nonExistentId = uuidV4();
frisby.create('GET new query that does not exist returns 404')
    .get(`http://localhost:8080/api/query/${nonExistentId}`)
    .expectStatus(404)
    .toss();

// Retrieve the query
frisby.create('GET new query')
    .get(`http://localhost:8080/api/query/${queryId}`)
    .expectStatus(200)
    .expectJSON (newQuery)
    // .inspectJSON()
    .expectHeaderContains('content-type', 'application/json')
    .toss();


// Search all queries

frisby.create('Search for all queries')
    .post(`http://localhost:8080/api/query/search`, {}, {json:true})
    .expectStatus(200)
    .expectJSONLength('hits.hits', 1)
    .expectHeaderContains('content-type', 'application/json')
    .toss();

// Find queries matching a person
// The person document that will get matched against
const testTag = uuidV4();
let p = new Person(uuidV4());
p.tags.push(testTag);
frisby.create('Create test person: POST api/person')
  .post('http://localhost:8080/api/person', p, {json: true})
  .expectStatus(201)
  .toss();

// The saved query:
let savedQuery = new QueryBody(uuidV4());
savedQuery.queryParameters.tags.push(testTag);

frisby.create('Create test query: POST query')
    .post('http://localhost:8080/api/query', savedQuery, {json: true})
    .expectStatus(201)
    .toss();

// The percolate search itself:
let percolateSearchBody = {
    entity: "person",
    id: p.id
};
frisby.create('Percolate queries')
    .post(`http://localhost:8080/api/query/searchPerc`, percolateSearchBody, {json:true})
    .expectStatus(200)
    .expectJSONLength('hits.hits', 1)
    .expectHeaderContains('content-type', 'application/json')
    .toss();
