#!/bin/bash

# Elasticsearch indexes that are used in the project.
# Having list ensures that other existing indexes are not deleted
# (useful when working on test databases)

esindexes=("person" "savedquery")
ES_DB="localhost:9200"

echo -e '\nINITIALIZING ELASTICSEARCH DATABASE'

echo -e '\n'Deleting indexes:
for i in "${esindexes[@]}"
do
    echo -e '\t'Deleting index $i
    curl -XDELETE -o /dev/null -s $ES_DB/$i
done

echo -e '\n'Create mappings:
for i in "${esindexes[@]}"
do
    INDEX_MAPPING="../../data/es-mapping/$i-mapping.json"
    if [ -e $INDEX_MAPPING ]
    then
        echo -e '\t'$i : $INDEX_MAPPING
        curl -XPUT --data "@$INDEX_MAPPING" -H "Content-Type: application/json" $ES_DB/$i
    fi
done

echo -e '\n' Adding data...
# cat search-test-data.bulk | curl -XPOST $ES_DB/_bulk -o /dev/null -s -S --data-binary @-
cat search-test-data.bulk | curl -XPOST $ES_DB/_bulk -s -S --data-binary @-

echo -e '\n\nINITIALIZATION COMPLETE\n'

