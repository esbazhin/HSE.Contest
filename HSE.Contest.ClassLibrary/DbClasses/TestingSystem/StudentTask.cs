﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.TestingSystem
{
    public class StudentTask
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "groupId")]
        public int? GroupId { get; set; }

        [Column(name: "tl")]
        public int? TimeLimit { get; set; }

        [Column(name: "taskText", TypeName = "text")]
        string TaskText { get; set; }

        [Column(name: "from", TypeName = "timestamptz")]
        public DateTime From { get; set; } = DateTime.Now.Date;
        [Column(name: "to", TypeName = "timestamptz")]
        public DateTime To { get; set; } = DateTime.Now.Date.AddMinutes(20);

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
        };

        public string ConvertToJson()
        {
            var json = JsonConvert.SerializeObject(this, serializerSettings);
            return json;
        }

        public void LoadJson(string json)
        {
            //var dateTimeConverter = new IsoDateTimeConverter();
            var ob = JsonConvert.DeserializeObject<StudentTask>(json, serializerSettings);
            Id = ob.Id;
            Name = ob.Name;
        }
    } 
}