using System;
using Hdq.PersonDataManager.Api.Modules;
using Newtonsoft.Json.Linq;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class QueryAggregations
    {
        public static JProperty GetGeoDistanceAggregation(Coord coord)
        {
            string s = 
                @"{
                    ""geo_distance"" : {
                        ""field"": ""geo.coord"",
                        ""origin"": " + $"{coord.Lat}, {coord.Lon}".Enclose() + @",
                        ""unit"": ""km"",
                        ""ranges"": [
                            {""to"": 10},
                            {""from"": 10, ""to"": 20},
                            {""from"": 20, ""to"": 50},
                            {""from"": 50, ""to"": 500},
                            {""from"": 500}
                        ]
                    }
                }";
            return new JProperty("geoDistance", JObject.Parse(s));
        }

        public static JProperty GetGeoGridAggregation()
        {
            string s =
                @"{
                    ""geohash_grid"" : {
                        ""field"" : ""address.geo.coord"",
                        ""precision"" : 3
                    }
                }";
            return new JProperty("geoGrid", JObject.Parse(s));
        }

        public static JProperty GetTagAggregations()
        {
            string obj = 
                @"{
                        ""terms"" : {
                            ""field"" : ""tags.keyword""
                        }
                    }";

            return new JProperty("tagAggs", JObject.Parse(obj));
        }
        
        
        public static void AddIf(this JObject o, bool conditional, Func<JProperty> p)
        {
            if (conditional)
                o.Add(p());
        }
        
        
        public static JProperty GetPoolStatusAggregations()
        {
            string obj =
                @"{
                    ""nested"": {
                        ""path"": ""poolStatuses""
                    },
                    ""aggs"": {
                        ""poolStatusAggs"": {
                            ""terms"": {
                                ""field"": ""poolStatuses.pool.id""
                            },
                            ""aggs"": {
                                ""pool.description"": {
                                    ""terms"": {
                                        ""field"": ""poolStatuses.pool.description"",
                                        ""size"": 1
                                    }
                                },
                                ""statusAgg"": {
                                    ""terms"": {
                                        ""field"": ""poolStatuses.status""
                                    }
                                }
                            }
                        }
                    }
                  }";
            
            return new JProperty("poolStatusAggs", JObject.Parse(obj));

        }

    }
}