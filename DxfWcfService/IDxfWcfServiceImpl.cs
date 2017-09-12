using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.IO;

namespace DxfWcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IDxfWcfServiceImpl" in both code and config file together.
    [ServiceContract]
    public interface IDxfWcfServiceImpl
    {
        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "/Layers")]
        Stream GetLayers(Stream data);

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "/Booths")]
        Stream GetBoothsInfo(Stream data);

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "/LoadFile")]
        Stream LoadFile(Stream data);

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare,
            UriTemplate = "/GetJpeg")]
        Stream GetPictures(Stream data);
    }
}
