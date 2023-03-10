//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Tabula
{
    [Serializable]
    public class UpdateMsgMsg : Message
    {
        public const string k_RosMessageName = "tabula_msgs/UpdateMsg";
        public override string RosMessageName => k_RosMessageName;

        public int msg_id;
        public string update;

        public UpdateMsgMsg()
        {
            this.msg_id = 0;
            this.update = "";
        }

        public UpdateMsgMsg(int msg_id, string update)
        {
            this.msg_id = msg_id;
            this.update = update;
        }

        public static UpdateMsgMsg Deserialize(MessageDeserializer deserializer) => new UpdateMsgMsg(deserializer);

        private UpdateMsgMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.msg_id);
            deserializer.Read(out this.update);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.msg_id);
            serializer.Write(this.update);
        }

        public override string ToString()
        {
            return "UpdateMsgMsg: " +
            "\nmsg_id: " + msg_id.ToString() +
            "\nupdate: " + update.ToString();
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
