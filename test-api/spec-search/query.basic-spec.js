const frisby = require('frisby');
const uuidV4 = require('uuid/v4');

// Create a query

class QueryBody {
    constructor(id) {
        this.query = {
            name: {
                "firstName": "",
                "lastName": ""
            },
            poolStatuses : [],
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
frisby.create('GET new query')
    .get(`http://localhost:8080/api/query/${uuidV4()}`)
    .expectStatus(404)
    .toss();

// Retrieve the query
frisby.create('GET new query')
    .get(`http://localhost:8080/api/query/${queryId}`)
    .expectStatus(200)
    .expectJSON (newQuery)
    .expectHeaderContains('content-type', 'application/json')
    .toss();


// Search all queries

frisby.create('SEARCH all queries')
    .post(`http://localhost:8080/api/query/search`, {}, {json:true})
    .expectStatus(200)
    .expectJSONLength('hits.hits', 1)
    .expectHeaderContains('content-type', 'application/json')
    .toss();

// Find queries matching a person
// Create the person.