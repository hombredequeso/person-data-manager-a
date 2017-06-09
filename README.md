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

WINDOWS USERS: the following uses linux commands/shell scripts. It needs to be run in linux like environement, such as 'git bash' or the Windows Linux Subsystem.

### Prerequisites

1. [jq](https://stedolan.github.io/jq/). Needs to be accessible via the current path.
2. Elasticsearch at localhost:9200.

### Data Generation

```
cd data-generator
npm install
curl -XDELETE -o /dev/null -s localhost:9200/person
curl -XPUT --data '@../data/person-mapping.json' -o /dev/null -s localhost:9200/person
node index.js --count 100 | jq -c '.[] | {"index": {"_index": "person", "_type": "person", "_id": .id|tostring }}, .' | curl -XPOST localhost:9200/_bulk -o /dev/null --data-binary @-
```

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

