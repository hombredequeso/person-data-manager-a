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

## Running Tests

Api tests using the [frisbyjs](http://frisbyjs.com/) nodejs framework are available. To run them, ensure Elasticsearch is running at localhost:9200.
To run the tests the first time:

```
npm install -g jasmine-node
cd api-test
npm install
jasmine-node spec
```

There-after:
```
cd api-test
jasmine-node spec

```

## Generating Sample Data

WINDOWS USERS: the following uses linux commands/shell scripts. 

### Prerequisites

1. Get the [person-random-data-generator](https://github.com/hombredequeso/person-random-data-generator). Have it in the same directory as this project.
2. Install [jq](https://stedolan.github.io/jq/)
3. Startup Elasticsearch at localhost:9200.

### Data Generation

First, a bash shell script can be used. On Windows your best bet is the Windows Linux Subsystem.
The person index can be created and populated from within the data directory:
```
./resetdb.sh
```

Second, a more manual approach can be used.
This will work within a bash shell, or a git bash shell.

1. Copy data/person-data-manager-generator-1.js into the person-random-data-generator util directory.
2. Run the following commands from the data directory, which will create 10,000 entries in the person index.

```
curl -XDELETE localhost:9200/person 
curl -XPUT --data '@./person-mapping.json' localhost:9200/person 
node ../../person-random-data-generator/util/create-people.js --generator ./person-data-manager-generator-1.js --count 10000 | jq -c '.[] | {"index": {"_index": "person", "_type": "person"}}, .' | curl -XPOST localhost:9200/_bulk -o /dev/null --data-binary @-
```


## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

