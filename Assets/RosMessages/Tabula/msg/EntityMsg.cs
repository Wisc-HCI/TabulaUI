//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Tabula
{
    [Serializable]
    public class EntityMsg : Message
    {
        public const string k_RosMessageName = "tabula_msgs/Entity";
        public override string RosMessageName => k_RosMessageName;

        public string name;
        public string[] categories;
        public string entity_class;

        public EntityMsg()
        {
            this.name = "";
            this.categories = new string[0];
            this.entity_class = "";
        }

        public EntityMsg(string name, string[] categories, string entity_class)
        {
            this.name = name;
            this.categories = categories;
            this.entity_class = entity_class;
        }

        public static EntityMsg Deserialize(MessageDeserializer deserializer) => new EntityMsg(deserializer);

        private EntityMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.name);
            deserializer.Read(out this.categories, deserializer.ReadLength());
            deserializer.Read(out this.entity_class);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.name);
            serializer.WriteLength(this.categories);
            serializer.Write(this.categories);
            serializer.Write(this.entity_class);
        }

        public override string ToString()
        {
            return "EntityMsg: " +
            "\nname: " + name.ToString() +
            "\ncategories: " + System.String.Join(", ", categories.ToList()) +
            "\nentity_class: " + entity_class.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
