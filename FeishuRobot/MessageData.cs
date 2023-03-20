namespace FeishuRobot
{
    class MessageData
    {
        //public string timestamp;
        //public string sign;
        public string msg_type;
        public MessageContent content;

        public MessageData(string msg_type, MessageContent content)
        {
            this.msg_type = msg_type;
            this.content = content;
        }


        //public MessageData(string timestamp,string sign,string msg_type, MessageContent content)
        //{
        //    this.timestamp = timestamp;
        //    this.sign = sign;
        //    this.msg_type = msg_type;
        //    this.content = content;
        //}
    }
}
