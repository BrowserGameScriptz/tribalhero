﻿package src.Comm.Commands 
{
	import src.Comm.*;
	import src.Map.Map;
	import src.Map.MapComm;
	
	public class MessageBoardComm
	{
		private var mapComm: MapComm;		
		private var session: Session;
			
		public function MessageBoardComm(mapComm: MapComm) {
			this.mapComm = mapComm;			
			this.session = mapComm.session;
		}
		
		public function dispose() : void {
		
		}
		
		public function listing(loader: GameURLLoader, page: int) : void {
			loader.load("/message_boards/listing", [ { key: "page", value: page }]);
		}
		
		public function view(loader: GameURLLoader, page: int, threadId: int) : void {
			loader.load("/message_boards/view", [ { key: "page", value: page }, { key: "threadId", value: threadId }]);
		}
		
		public function delThread(loader: GameURLLoader, page: int, threadId: int) : void {
			loader.load("/message_boards/del_thread", [ { key: "page", value: page }, { key: "threadId", value: threadId }]);
		}
		
		public function delPost(loader: GameURLLoader, page: int, postId: int) : void {
			loader.load("/message_boards/del_thread", [ { key: "page", value: page }, { key: "postId", value: postId }]);
		}		
		
		public function addThread(loader: GameURLLoader, page: int, subject: String, message: String) : void {
			loader.load("/message_boards/add_thread", [ { key: "page", value: page }, { key: "subject", value: subject }, { key: "message", value: message}]);
		}		
		
		public function addPost(loader: GameURLLoader, page: int, threadId: int, subject: String, message: String) : void {
			loader.load("/message_boards/add_thread", [ { key: "page", value: page }, { key: "threadId", value: threadId }, { key: "subject", value: subject }, { key: "message", value: message}]);
		}							
	}

}