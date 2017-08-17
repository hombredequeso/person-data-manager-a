const frisby = require('frisby');
const uuidV4 = require('uuid/v4');

const Person = require('./person-builder').Person;
const Address = require('./person-builder').Address;

class Query {
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
let newQuery = new Query(queryId);

newQuery.queryParameters.tags.push(uuidV4());

frisby.create(`Create a new query: ${queryId}`)
    .post('http://localhost:8080/api/query?refresh', newQuery, {json: true})
    .expectStatus(201)
    .toss();


let nonExistentId = uuidV4();
frisby.create('GET query that does not exist')
    .get(`http://localhost:8080/api/query/${nonExistentId}`)
    .expectStatus(404)
    .toss();

frisby.create('GET existing query: ${queryId}')
    .get(`http://localhost:8080/api/query/${queryId}`)
    .expectStatus(200)
    .expectJSON (newQuery)
    .expectHeaderContains('content-type', 'application/json')
    .toss();


frisby.create('Search for all queries')
    .post(`http://localhost:8080/api/query/search`, {}, {json:true})
    .expectStatus(200)
    .expectJSONLength('hits.hits', 1)
    .expectHeaderContains('content-type', 'application/json')
    .toss();

// Find queries matching a person

const testTag = uuidV4();
let p = new Person(uuidV4());
p.tags.push(testTag);
frisby.create(`Test setup: Create test person: ${p.id}`)
  .post('http://localhost:8080/api/person?refresh', p, {json: true})
  .expectStatus(201)
  .toss();

let savedQuery = new Query(uuidV4());
savedQuery.queryParameters.tags.push(testTag);

frisby.create(`Test setup: Create test query matching person ${p.id}, query id: ${savedQuery.metadata.id}`)
    .post('http://localhost:8080/api/query?refresh', savedQuery, {json: true})
    .expectStatus(201)
    .toss();

let percolateSearchBody = {
    entity: "person",
    id: p.id
};

let expectedPercResult = {
    hits: {
        hits: [
            {
                _id: savedQuery.metadata.id
            }
        ]
    }
};

frisby.create(`Search for queries matching person ${p.id}`)
    .post(`http://localhost:8080/api/query/searchPerc`, percolateSearchBody, {json:true})
    .expectStatus(200)
    .expectJSON (expectedPercResult)
    .expectJSONLength('hits.hits', 1)
    .expectHeaderContains('content-type', 'application/json')
    .toss();

