const frisby = require('frisby');

class SearchBody {
    constructor() {
        this.name= {
            "firstName": "",
            "lastName": ""
        };
        this.poolStatuses = [];
        this.tags= [];
        this.near= {
            "coord": {
                "lat": -37.814,
                "lon": 144.963
            },
            "distance" : 40
        };
    }
}

class Pool {
    constructor(id, description) {
        this.id = id;
        this.description = description;
    }
}

class PoolStatus {
    constructor(pool, status) {
        this.pool = pool;
        this.status = status;
    }
}

// Completely unique test
var uniqueTest = new SearchBody();
uniqueTest.name.firstName = "john";
uniqueTest.tags.push("tag1");
uniqueTest.near.coord.lat = -33.868;
uniqueTest.near.coord.lon = 151.207;
frisby.create('Search all fields with unique value')
    .post('http://localhost:8080/api/person/search', uniqueTest, {json: true})
    .expectStatus(200)
    .expectJSON('hits.hits.*', {
        _id : "e7147eb5-ea9e-411f-af93-b34e7895fa7e" 
    })
    .expectJSONLength('hits.hits', 1)
    .toss();

// Tag tests:

var tt1 = new SearchBody();
tt1.name.firstName = "bob";
tt1.tags.push("tagtest1");
tt1.near = null;
frisby.create('tagtest1')
    .post('http://localhost:8080/api/person/search', tt1, {json: true})
    .expectStatus(200)
    .expectJSON('hits.hits.*', {
        _id : "d7cca43b-3a17-42f3-a34d-7b19b2686a3a" 
    })
    .expectJSONLength('hits.hits', 1)
    .toss();

var tt2 = new SearchBody();
tt2.name.firstName = "bob";
tt2.tags.push("tagtest3");
tt2.near = null;
frisby.create('tagtest2')
    .post('http://localhost:8080/api/person/search', tt2, {json: true})
    .expectStatus(200)
    // hits:
    .expectJSONLength('hits.hits', 2)
    .expectJSON('hits.hits.?', {
        _id : "d5ee1c1f-8bbf-44e5-b08b-aca985e3657d" 
    })
    .expectJSON('hits.hits.?', {
        _id : "08076ae2-84dc-444f-875d-0c66bbdcb5ea" 
    })
    // tag aggregations:
    .expectJSONLength('aggregations.tagAggs.buckets', 3)
    .expectJSON('aggregations.tagAggs.buckets.?', {
       "key" : "tagtest3",
       "doc_count" : 2
    })
    .expectJSON('aggregations.tagAggs.buckets.?', {
          "key" : "tagtest4",
          "doc_count" : 1
    })
    .expectJSON('aggregations.tagAggs.buckets.?', {
          "key" : "tagtest5",
          "doc_count" : 1
    })
    .toss();

var tt2b = new SearchBody();
tt2b.name.firstName = "bob";
tt2b.tags.push("tagtest3");
tt2b.near = null;
tt2b.tags.push('tagtest4')
frisby.create('tagtest2b')
    .post('http://localhost:8080/api/person/search', tt2b, {json: true})
    .expectStatus(200)
    // hits:
    .expectJSONLength('hits.hits', 1)
    .expectJSON('hits.hits.?', {
        _id : "08076ae2-84dc-444f-875d-0c66bbdcb5ea" 
    })
    // tag aggregations:
    .expectJSONLength('aggregations.tagAggs.buckets', 2)
    .expectJSON('aggregations.tagAggs.buckets.?', {
       "key" : "tagtest3",
       "doc_count" : 1
    })
    .expectJSON('aggregations.tagAggs.buckets.?', {
          "key" : "tagtest4",
          "doc_count" : 1
    })
    .toss();

var tt3 = new SearchBody();
tt3.name.firstName = "bob";
tt3.tags.push("tagtest3");
tt3.near = null;
tt3.postfilter = {
    tags: ["tagtest4"]
};
frisby.create('post-filter test')
    .post('http://localhost:8080/api/person/search', tt3, {json: true})
    .expectStatus(200)
    // hits:
    .expectJSONLength('hits.hits', 1)
    .expectJSON('hits.hits.?', {
        _id : "08076ae2-84dc-444f-875d-0c66bbdcb5ea" 
    })
    // tag aggregations:
    .expectJSONLength('aggregations.tagAggs.buckets', 3)
    .expectJSON('aggregations.tagAggs.buckets.?', {
       "key" : "tagtest3",
       "doc_count" : 2
    })
    .expectJSON('aggregations.tagAggs.buckets.?', {
          "key" : "tagtest4",
          "doc_count" : 1
    })
    .expectJSON('aggregations.tagAggs.buckets.?', {
          "key" : "tagtest5",
          "doc_count" : 1
    })
    .toss();

var poolTest1 = new SearchBody();
poolTest1.poolStatuses.push(new PoolStatus(new Pool("f5ee1c1f-8bbf-44e5-b08b-aca985e36571", "pool1"), "STATUS_1"))
poolTest1.near = null;
frisby.create('pool test status_1')
    .post('http://localhost:8080/api/person/search', poolTest1, {json: true})
    // hits:
    .expectJSONLength('hits.hits', 1)
    .expectJSON('hits.hits.?', {
        _id :"e5ee1c1f-8bbf-44e5-b08b-aca985e3657a"
    })
    .expectStatus(200)
    .toss();

var poolTest2 = new SearchBody();
poolTest2.poolStatuses.push(new PoolStatus(new Pool("f5ee1c1f-8bbf-44e5-b08b-aca985e36571", "pool1"), "STATUS_2"))
poolTest2.near = null;
frisby.create('pool test status_2')
    .post('http://localhost:8080/api/person/search', poolTest2, {json: true})
    .expectJSONLength('hits.hits', 1)
    .expectJSON('hits.hits.?', {
        _id :"e5ee1c1f-8bbf-44e5-b08b-aca985e3657b"
    })
    .expectStatus(200)
    .toss();

var poolTest3 = new SearchBody();
poolTest3.poolStatuses.push(new PoolStatus(new Pool("f5ee1c1f-8bbf-44e5-b08b-aca985e36571", "pool1"), ""))
poolTest3.near = null;
frisby.create('pool test all statuses')
    .post('http://localhost:8080/api/person/search', poolTest3, {json: true})
    .expectJSONLength('hits.hits', 2)
    .expectJSON('hits.hits.?', {
        _id :  "e5ee1c1f-8bbf-44e5-b08b-aca985e3657a" 
    })
    .expectJSON('hits.hits.?', {
        _id : "e5ee1c1f-8bbf-44e5-b08b-aca985e3657b"
    })
    .expectStatus(200)
    .toss();
