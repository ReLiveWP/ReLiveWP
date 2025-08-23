using System.Xml.Serialization;

namespace ReLiveWP.Services.Activity.Models.Atom;

[XmlInclude(typeof(LiveReplyReaction))]
public class LiveReaction { }
