﻿using System;
using System.Collections.Generic;
using RESTable.OData;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable.SQLite.Example
{
    /// <summary>
    /// The application from RESTable.Tutorial, but modified to use OData as well. Run the 
    /// application and send OData requests (e.g. GET "127.0.0.1:8282/api-odata").
    /// </summary>
    public class TutorialApp
    {
        public static void Main()
        {
            RESTableConfig.Init
            (
                port: 8282,
                uri: "/api",
                requireApiKey: true,
                configFilePath: "/Config.xml",
                entityResourceProviders: new[] {new SQLiteEntityResourceProvider("\\data")},
                protocolProviders: new[] {new ODataProtocolProvider()}
            );

            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
            // The 'requireApiKey' parameter is set to 'true'. API keys are required in all incoming requests.
            // The 'configFilePath' points towards the configuration file, which contains API keys. In this case,
            //   this file is located in the project folder.
            // The 'resourceProviders' parameter is used for SQLite integration (see the ExampleDatabase class below)
            // The 'protocolProviders' parameter is used for activating OData support

            ExampleDatabase.Setup();
        }
    }

    [RESTable(GET, POST, PUT, PATCH, DELETE)]
    public class Superhero
    {
        public string Name { get; set; }
        public bool HasSecretIdentity { get; set; }
        public string Gender { get; set; }
        public int? YearIntroduced { get; set; }
        public DateTime InsertedAt { get; }
        public Superhero() => InsertedAt = DateTime.UtcNow;
    }

    [RESTable(GET)]
    public class SuperheroReport : IAsyncSelector<SuperheroReport>
    {
        public long NumberOfSuperheroes { get; private set; }
        public Superhero FirstSuperheroInserted { get; private set; }
        public Superhero LastSuperheroInserted { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// This method returns an IEnumerable of the resource type. RESTable will call this 
        /// on GET requests and send the results back to the client as e.g. JSON.
        /// </summary>
        public IEnumerable<SuperheroReport> Select(IRequest<SuperheroReport> request)
        {
//            var superHeroesOrdered = Db
//                .SQL<Superhero>("SELECT t FROM RESTable.Tutorial.Superhero t")
//                .OrderBy(h => h.InsertedAt)
//                .ToList();
//            return new[]
//            {
//                new SuperheroReport
//                {
//                    NumberOfSuperheroes = Db
//                        .SQL<long>("SELECT COUNT(t) FROM RESTable.Tutorial.Superhero t")
//                        .FirstOrDefault(),
//                    FirstSuperheroInserted = superHeroesOrdered.FirstOrDefault(),
//                    LastSuperheroInserted = superHeroesOrdered.LastOrDefault(),
//                }
//            };

            yield break;
        }
    }

    #region Demo database

    /// <summary>
    /// Database is a subset of https://github.com/fivethirtyeight/data/tree/master/comic-characters
    /// - which is, in turn, taken from Marvel and DC Comics respective sites.
    /// </summary>
    internal static class ExampleDatabase
    {
        internal static void Setup()
        {
            // First we delete all Superheroes from the database. Then we get the content from an included SQLite 
            // database and build the Starcounter database from it. For more information on how to integrate SQLite 
            // with RESTable, see the 'RESTable.SQLite' package on NuGet.

            // Db.Transact(() => Db
            //     .SQL<Superhero>("SELECT t FROM RESTable.Tutorial.Superhero t")
            //     .ForEach(Db.Delete));
// 
            // var request = Context.Root.CreateRequest<SuperheroSQLite>();
            // request.Conditions.Add("Year", Operators.NOT_EQUALS, null);
            // request.EvaluateToEntities().ForEach(hero => Db.Transact(() => new Superhero
            // {
            //     Name = hero.Name,
            //     YearIntroduced = hero.Year != 0 ? hero.Year : default(int?),
            //     HasSecretIdentity = hero.Id == "Secret Identity",
            //     Gender = hero.Sex == "Male Characters" ? "Male" : hero.Sex == "Female Characters" ? "Female" : "Other",
            // }));
        }
    }

    [SQLite(CustomTableName = "Heroes"), RESTableInternal(GET)]
    public class SuperheroSQLite : SQLiteTable
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Sex { get; set; }
        public int Year { get; set; }
    }

    #endregion
}