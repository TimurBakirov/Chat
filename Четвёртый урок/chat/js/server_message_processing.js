var ip = "127.0.0.1";
var port = "8080";
var socket;
var important;
var cookies;
var label;
var fieldOfSendMessage;
var nick;
var login_box;
var password;
var chatId = 1;
var labelFriends;

window.onload = function(){
	label = document.getElementById("listOfMessages");
	labelFriends = document.getElementById("listOfFriends");
	fieldOfSendMessage = document.getElementById("fieldOfSendMessage");
	login_box = document.getElementById("login");
	nick = document.getElementById("name");
	password = document.getElementById("password");
	PagesController(0);
	socket = new WebSocket("ws://"+ ip +":"+ port);
	socket.onopen = function(){
		console.log("Соединение установлено");
		if(document.cookie != "")
			socket.send(document.cookie);
		else
			login_box.style.display = "block";
	};
	socket.onclose = function(event){
		if(event.wasClean){
			console.log("Соединение закрыто чисто");
		}else{
			console.log("Обрыв соединения");
		}
	};
	socket.onmessage = function(event){
		important = JSON.parse(event.data);
		cookies = JSON.parse(document.cookie);
		switch(important["type"]){
			case 0:
				if(important["clear"] == null){
					if(chatId == important["chatId"] && important["chatId"] > 0 && chatId > 0){
						if(cookies["nick"] == important["nameUser"])
							label.innerHTML += "<div class='my_message'>"+ important["message"] +"</div>";
						else
							label.innerHTML += "<div class='friend_message'>"+ important["message"] +"</div>";
						listOfMessages.scrollTop = listOfMessages.scrollHeight;
					}
				}
				else{
					label.innerHTML = "";
					chatId = important["chatId"];
				}
				switch(important["errorCode"]){
					case 0:
						PagesController(0);
						break;
				}
				break;
			case 1:
				if(important["error"] == 0){
					label.innerHTML += "<div class='info_message'>Вы авторизировались!</div>";
					fieldOfSendMessage.disabled = "";
					login_box.style.display = "none";
				}
				if(important["error"] == 1)
					label.innerHTML += "<div class='info_message'>Неправильный логин или пароль!</div>";
				if(important["error"] == 2)
					label.innerHTML += "<div class='info_message'>Данный аккаунт уже занят!</div>";
				break;
			case 2:
				listOfFriends.innerHTML += "<div id='chatId_"+ important['chatId'] +"' class='friendBox' onclick='PagesController(1, "+ important['chatId'] +")'><div class='userIcon'></div><div class='userName'>"+ important['chatName'] +"</div><div class='lastMessage'>"+ important['lastMessage'] +"</div></div>";
				break;
		}
	};
	btnSend.onclick = function (){
		if (chatId > 0)
			socket.send('{"type":0,"message":"'+ fieldOfSendMessage.value +'", "chatId":"'+ chatId +'"}');
		fieldOfSendMessage.value = "";
	};
	fieldOfSendMessage.onkeydown = function () {
		if(event.keyCode == 13){
			if (chatId > 0)
				socket.send('{"type":0,"message":"'+ fieldOfSendMessage.value +'", "chatId":"'+ chatId +'"}');
			fieldOfSendMessage.value = "";
		}
	}
	btnAuth.onclick = function (){
		socket.send('{"type":1,"nick":"'+ nick.value +'","password":"'+ password.value +'"}');
		document.cookie = '{"type":1,"nick":"'+ nick.value +'","password":"'+ password.value +'"};path=/';
		nick.value = "";
		password.value = "";
	};
	nick.onkeydown = function () {
		if(event.keyCode == 13){
			socket.send('{"type":1,"nick":"'+ nick.value +'","password":"'+ password.value +'"}');
			document.cookie = '{"type":1,"nick":"'+ nick.value +'","password":"'+ password.value +'"};path=/';
			nick.value = "";
			password.value = "";
		}
	}
	password.onkeydown = function () {
		if(event.keyCode == 13){
			socket.send('{"type":1,"nick":"'+ nick.value +'","password":"'+ password.value +'"}');
			document.cookie = '{"type":1,"nick":"'+ nick.value +'","password":"'+ password.value +'"};path=/';
			nick.value = "";
			password.value = "";
		}
	}
}
function PagesController(page, idChat = 0){
	switch(page){
		case 0:
			chatId = 0;
			listOfMessagesBlock.style.display = "none";
			listOfFriendsBlock.style.display = "block";
			break;
		case 1:
			chatId = idChat;
			socket.send('{"type":2,"chatId":"'+ idChat +'"}');
			listOfMessagesBlock.style.display = "block";
			listOfFriendsBlock.style.display = "none";
			break;
	}
}