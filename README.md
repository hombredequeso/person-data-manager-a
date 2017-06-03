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

To run requires a local instance of Elasticsearch 5+ to be running.

[OWIN self-hosted](https://github.com/NancyFx/Nancy/wiki/Hosting-nancy-with-owin#katana---httplistener-selfhost)

This means Visual Studio needs to be run as admin, or follow the ["Running without admin" instructions](https://github.com/NancyFx/Nancy/wiki/Hosting-nancy-with-owin#katana---httplistener-selfhost)

## Generating Sample Data

Sample data for this project can be generated in the following way.
1. Get the [person-random-data-generator](https://github.com/hombredequeso/person-random-data-generator)
2. Install [jq](https://stedolan.github.io/jq/)
3. Startup Elasticsearch at localhost:9200.
3. Copy data/person-data-manager-generator-1.js into the person-random-data-generator util directory.
4. Run the following command from the person-random-data-generator/util directory:

```
node create-people.js --generator ./person-data-manager-generator-1.js | jq -c '.[] | {"index": {"_index": "person", "_type": "person"}}, .' | curl -XPOST localhost:9200/_bulk --data-bin ary @-

```
## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

