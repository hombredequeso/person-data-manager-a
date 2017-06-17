# Prototype: Person Data Manager in Elasticsearch

A project for managing person data, for the purposes of prototyping various technologies.

Example of technologies and functions getting prototyped:
* Elasticsearch 5.x, bulk update functionality.

Written using the [nancyfx](http://nancyfx.org/) web framework. 

## Getting Started

### Prerequisites

* Elasticsearch 5.+. If using docker, see [elasticsearch-docker](https://github.com/hombredequeso/elasticsearch-docker)
* To have data to play with, see *Generating Sample Data* below.

### Running Locally

To run requires a local instance of Elasticsearch 5.+ to be running.

The api framework is written using the [nancyfx](http://nancyfx.org/) web framework and is [OWIN self-hosted](https://github.com/NancyFx/Nancy/wiki/Hosting-nancy-with-owin#katana---httplistener-selfhost)

This means Visual Studio needs to be run as admin, or follow the ["Running without admin" instructions](https://github.com/NancyFx/Nancy/wiki/Hosting-nancy-with-owin#katana---httplistener-selfhost)

### Postman

The file data/person-data-manager.postman_collection.json contains a sample collection of sample api requests that can be loaded in to [Postman](https://www.getpostman.com/).

A simple test of these queries may go as follows:
* Populate the database as in *Generating Sample Data*.
* Execute *POST api/person* to create a new person (Sophia Mclaughlin).
* Execute *GET /api/person/{id}* to get the Sophia Mclaughlin.
* Execute *POST api/person/search* to search for people, which should include Sophia Mclaughlin.
* Execute *POST /api/person/morelike* to find more people like Sophia Mclaughlin
* Execute *POST api/person/tag* to update all people with the firstname "Sophia" and a tag "item3"

## API Tests

### Prerequisites

* Node 6+

### Running API Tests

Api tests using the [frisbyjs](http://frisbyjs.com/) nodejs framework are available. To run them, ensure Elasticsearch is running at localhost:9200.
The tests themselves use bash shell scripts to populate the data. These can be run in 'git bash' or the Windows Linux Subsystem.
Before running the tests for the first time:

```
npm install -g jasmine-node
cd test-api
npm install
```

Within the /test-api directory are a number of spec directories for different types of api tests.
* spec-bulk does very basic sanity tests on very large amounts of data
* spec-search does very targeted search tests on small amounts of data

To run any of the tests, change directory into the relevant spec directory and run as follows:
```
./resetdb.sh
jasmine-node .

```

## Generating Sample Data

WINDOWS USERS: the following uses linux commands/shell scripts. It needs to be run in linux like environement, such as 'git bash' or the Windows Linux Subsystem.

### Prerequisites

1. [jq](https://stedolan.github.io/jq/). Needs to be accessible via the current path.
2. Elasticsearch at localhost:9200.

### Data Generation
Sample data for the person-data-manager can be generated using the following in bash (Windows users on the linux subsystem).

```
cd data-generator
npm install
cd ../data
./resetdb.sh
```

This will create 10,000 random people in the elasticsearch /people index.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

