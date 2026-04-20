using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Services.MicroServices.PersistanceService
{
    public class LocalPersistenceContractResolver : DefaultContractResolver
    {
        private const BindingFlags BINDING_FLAGS = BindingFlags.Public | 
                                                   BindingFlags.NonPublic | 
                                                   BindingFlags.Instance |
                                                   BindingFlags.DeclaredOnly;
        
        protected override IList<JsonProperty> CreateProperties(Type p_type, MemberSerialization p_memberSerialization)
        {
            List<MemberInfo> l_members = new List<MemberInfo>();

            l_members.AddRange(p_type.GetFields(BINDING_FLAGS));
            l_members.AddRange(p_type.GetProperties(BINDING_FLAGS).Where(x => !x.GetMethod.IsAbstract));

            if (p_type.BaseType != null)
            {
                l_members.AddRange(p_type.BaseType.GetFields(BINDING_FLAGS));
                l_members.AddRange(p_type.BaseType.GetProperties(BINDING_FLAGS).Where(x => !x.GetMethod.IsAbstract));
            }

            l_members = l_members.Where(p_memberInfo => Attribute.IsDefined(p_memberInfo, typeof(SerializeField)) || 
                                                        Attribute.IsDefined(p_memberInfo, typeof(JsonPropertyAttribute)) || 
                                                        (p_memberInfo as FieldInfo)?.IsPublic == true).ToList();
            
            List<JsonProperty> l_properties = new List<JsonProperty>();

            foreach (MemberInfo l_member in l_members)
            {
                l_properties.Add(GetProperty(l_member, p_memberSerialization));
            }
            
            return l_properties;
        }

        private JsonProperty GetProperty(MemberInfo p_member, MemberSerialization p_memberSerialization)
        {
            JsonProperty l_property = CreateProperty(p_member, p_memberSerialization);
            l_property.Writable = true;
            l_property.Readable = true;
            return l_property;
        }
    }
}