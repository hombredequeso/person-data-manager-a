#!/bin/bash 
COUNTER=0
while [  $COUNTER -lt 100 ]; do

    curl -X POST \
      http://localhost:8080/api/person \
      -H 'content-type: application/json' \
      -d '{
      "id":"9cc62f6f-2650-4b37-b902-711d40a1bc32",
      "name": {
        "firstName": "Sophia",
        "lastName": "Mclaughlin"
      },
      "tags": [
        "item3",
        "item5",
        "item8"
      ],
      "poolStatuses": [],
      "contact": {
        "phone": [
          {
            "label": "home",
            "number": "07 0624 8545"
          }
        ],
        "email": [
          {
            "label": "work",
            "address": "Sophia.Mclaughlin@yahoo.com"
          }
        ]
      },
      "address": {
        "region": "Melbourne",
        "geo": {
          "coord": {
            "lat": -37.80576653851945,
            "lon": 144.94075561102966
          }
        }
      }
    }'


    echo The counter is $COUNTER
    let COUNTER=COUNTER+1 
done
