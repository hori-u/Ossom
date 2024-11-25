using System;
using System.Collections.Generic;
using System.Text;

namespace P2P {
	public class PeerLogger {
		public enum MessageLevel {
			Info,
			Warn,
			debug,
			Error,
			FatalError,
			Unknown,
		}

		public class MessageEventArgs : EventArgs {
			public MessageLevel Level = MessageLevel.Unknown;
			public string ID = string.Empty;
			public string Message = string.Empty;
			public DateTime LogTime = DateTime.Now;

			public MessageEventArgs(MessageLevel level, string id, string msg) {
				this.Level = level;
				this.ID = id;
				this.Message = msg;
			}

			public MessageEventArgs(string msg) {
				this.Level = MessageLevel.Unknown;
				this.ID = string.Empty;
				this.Message = msg;
			}
		}

		public delegate void MessageEventHandler(object sender, MessageEventArgs mea);

		public event MessageEventHandler MessagingEvent;

		public virtual void OnMassage(string msg) {
			if (MessagingEvent != null) {
				MessageEventArgs mea = new MessageEventArgs(msg);
				MessagingEvent(this, mea);
			}
		}

		public virtual void OnMassage(MessageLevel level, string id, string msg) {
			if (MessagingEvent != null) {
				MessageEventArgs mea = new MessageEventArgs(level, id, msg);
				MessagingEvent(this, mea);
			}
		}

		public virtual void OnMassage(MessageEventArgs mea) {
			if (MessagingEvent != null) {
				MessagingEvent(this, mea);
			}
		}

		//シングルトン
		private PeerLogger() { }

		static public PeerLogger Instance = new PeerLogger();
	}
}
