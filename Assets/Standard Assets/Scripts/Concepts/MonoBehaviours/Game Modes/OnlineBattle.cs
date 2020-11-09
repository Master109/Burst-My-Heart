﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;
using UnityEngine.UI;
using PlayerIOClient;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace BMH
{
	//[ExecuteAlways]
	public class OnlineBattle : GameMode
	{
		public new static OnlineBattle instance;
		public new static OnlineBattle Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<OnlineBattle>();
				return instance;
			}
		}
		public const uint DATA_PER_EVENT = 4;
		public static OnlinePlayer localPlayer;
		public static OnlinePlayer[] nonLocalPlayers = new OnlinePlayer[0];
		public static SortedDictionary<int, OnlinePlayer> playerIdsDict = new SortedDictionary<int, OnlinePlayer>();
		public static bool isWaitingForAnotherPlayer;
		public static bool isPlaying;
		public float bountyMultiplier = 1;
		public OnlinePlayer playerPrefab;
		// public Text loadingText;
		List<Message> spawnPlayerMessages = new List<Message>();
		List<Message> makeEventMessages = new List<Message>();
		public List<Event> events = new List<Event>();
		[Multiline]
		public string isWaitingForAnotherPlayerText;
		[Multiline]
		public string isNotWaitingForAnotherPlayerText;

		public override void Awake ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			// Connect ();
		}

		public virtual void Connect ()
		{
			NetworkManager.Instance.Connect (OnAuthenticateSucess, OnAuthenticateFail);
		}

		public virtual void OnAuthenticateSucess (Client client)
		{
			// Debug.Log("OnAuthenticateSucess");
			NetworkManager.client = client;
			// NetworkManager.client.Multiplayer.GameServerEndpointFilter = SetConnectEndpoint;
			NetworkManager.client.Multiplayer.UseSecureConnections = true;
			CreateJoinRoom ();
		}

		public virtual void OnAuthenticateFail (PlayerIOError error)
		{
			// Debug.Log("OnAuthenticateFail: " + error.ToString());
			NetworkManager.Instance.notificationTextObject.text.text = "Error: " + error.ToString();
			NetworkManager.Instance.StartCoroutine(NetworkManager.Instance.notificationTextObject.DisplayRoutine ());
			// Connect ();
		}

		public virtual void CreateJoinRoom ()
		{
			NetworkManager.client.Multiplayer.CreateJoinRoom("Default",
				"Burst My Heart",
				true,
				null,
				null,
				OnCreateJoinRoomSucess,
				OnCreateJoinRoomFail
			);
		}

		public virtual void OnCreateJoinRoomSucess (Connection connection)
		{
			// Debug.Log("OnCreateJoinRoomSucess");
			NetworkManager.IsOnline = true;
			NetworkManager.connection = connection;
			NetworkManager.connection.OnMessage += OnMessage;
		}

		public virtual void OnCreateJoinRoomFail (PlayerIOError error)
		{
			// Debug.Log("OnCreateJoinRoomFail: " + error.ToString());
			CreateJoinRoom ();
		}

		public virtual void OnMessage (object sender, Message message)
		{
			// Debug.Log(message.Type);
			switch (message.Type)
			{
				case "Spawn Player":
					OnSpawnPlayerMessage (sender, message);
					break;
				case "Move Transform":
					OnMoveTransformMessage (sender, message);
					break;
				case "Change Score":
					OnChangeScoreMessage (sender, message);
					break;
				case "Remove Player":
					OnRemovePlayerMessage (sender, message);
					break;
				case "Make Events":
					OnMakeEventsMessage (sender, message);
					break;
				case "Event Done":
					OnEventDoneMessage (sender, message);
					break;
			}
		}

		public virtual void OnSpawnPlayerMessage (object sender, Message message)
		{
			if (isPlaying)
				SpawnPlayer (message);
			else
			{
				spawnPlayerMessages.Add(message);
				if (isWaitingForAnotherPlayer && spawnPlayerMessages.Count > 1)
				{
					isWaitingForAnotherPlayer = false;
					OnAnotherPlayerJoined ();
				}
			}
		}

		public virtual void OnAnotherPlayerJoined ()
		{
			if (SinglePlayerGameMode.Instance != null || SceneManager.GetActiveScene().name.Contains("AI"))
				SceneManager.sceneLoaded += OnSceneLoaded;
			else
				StartCoroutine(LoadOnlineArenaAfterNotificationRoutine ());
		}

		public virtual IEnumerator LoadOnlineArenaAfterNotificationRoutine ()
		{
			NetworkManager.Instance.notificationTextObject.text.text = "Another player has joined online! You will be transported to the online arena after this message disappears.";
			NetworkManager.Instance.StopCoroutine(NetworkManager.Instance.notificationTextObject.DisplayRoutine ());
			NetworkManager.Instance.notificationTextObject.Show ();
			NetworkManager.Instance.StartCoroutine(NetworkManager.Instance.notificationTextObject.DisplayRoutine ());
			yield return new WaitUntil(() => (!NetworkManager.Instance.notificationTextObject.obj.activeSelf));
			SceneManager.sceneLoaded += OnSceneLoaded;
			GameManager.Instance.LoadScene ("Online");
		}

		public virtual void OnSceneLoaded (Scene scene = new Scene(), LoadSceneMode loadMode = LoadSceneMode.Single)
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			if (OnlineArena.Instance == null)
			{
				StartCoroutine(LoadOnlineArenaAfterNotificationRoutine ());
				return;
			}
			// GameManager.Instance.PauseGame (100);
			// NetworkManager.Instance.notificationTextObject.text.text = "Waiting for the other player to be trasported to this arena...";
			// NetworkManager.Instance.StopCoroutine(NetworkManager.Instance.notificationTextObject.DisplayRoutine ());
			// NetworkManager.Instance.StartCoroutine(NetworkManager.Instance.notificationTextObject.DisplayRoutine ());
			pauseInstructionsObj.SetActive(!HasPaused);
			foreach (Message spawnPlayerMessage in spawnPlayerMessages)
				SpawnPlayer (spawnPlayerMessage);
			spawnPlayerMessages.Clear();
			foreach (Message makeEventMessage in makeEventMessages)
				MakeEvents (makeEventMessage);
			makeEventMessages.Clear();
			isPlaying = true;
		}

		public virtual void SpawnPlayer (Message message)
		{
			OnlinePlayer player;
			if (message.Count == 4)
			{
				// player = ObjectPool.Instance.SpawnComponent<OnlinePlayer>(playerPrefab.prefabIndex, new Vector2(message.GetFloat(1), message.GetFloat(2)), Quaternion.LookRotation(Vector3.forward, VectorExtensions.FromFacingAngle(message.GetFloat(3))));
				player = Instantiate(playerPrefab, new Vector2(message.GetFloat(1), message.GetFloat(2)), Quaternion.LookRotation(Vector3.forward, VectorExtensions.FromFacingAngle(message.GetFloat(3))));
				OnlineBattle.localPlayer = player;
				player.owner = GameManager.Instance.teams[0];
				player.SetColor (player.owner.color);
				player.scoreText.text = "Score: " + player.Score;
				GameManager.updatables = GameManager.updatables.Add(this);
				// loadingText.text = "Please wait for another player to join";
				if (!HasPaused)
					pauseInstructionsObj.SetActive(true);
			}
			else
			{
				// player = ObjectPool.Instance.SpawnComponent<OnlinePlayer>(playerPrefab.prefabIndex);
				player = Instantiate(playerPrefab);
				player.body.trs.position = new Vector2(message.GetFloat(1), message.GetFloat(2));
				player.weapon.trs.position = new Vector2(message.GetFloat(3), message.GetFloat(4));
				player.ChangeScore (message.GetUnsignedInteger(5));
				player.enabled = false;
				GameManager.updatables = GameManager.updatables.Remove(player);
				OnlineBattle.nonLocalPlayers = OnlineBattle.nonLocalPlayers.Add(player);
				// loadingText.enabled = false;
			}
			player.playerId = message.GetInteger(0);
			playerIdsDict.Add(player.playerId, player);
			OnlineArena.Instance.SetSize ((uint) playerIdsDict.Count);
		}

		public virtual void OnMoveTransformMessage (object sender, Message message)
		{
			OnlinePlayer player;
			if (!playerIdsDict.TryGetValue(message.GetInteger(0), out player))
				return;
			if (message.GetBoolean(1))
				player.body.trs.position = new Vector2(message.GetFloat(2), message.GetFloat(3));
			else
				player.weapon.trs.position = new Vector2(message.GetFloat(2), message.GetFloat(3));
			// if (OnlinePlayer.localPlayer == player)
			// {
			// 	GameManager.Instance.PauseGame (-100);
			// 	NetworkManager.Instance.notificationTextObject.Hide ();
			// }
		}

		public virtual void OnChangeScoreMessage (object sender, Message message)
		{
			OnlinePlayer player;
			if (!playerIdsDict.TryGetValue(message.GetInteger(0), out player))
				return;
			player.ChangeScore (message.GetUnsignedInteger(1));
		}

		public virtual void OnRemovePlayerMessage (object sender, Message message)
		{
			int playerId = message.GetInteger(0);
			OnlinePlayer player;
			if (!playerIdsDict.TryGetValue(playerId, out player))
				return;
			Destroy(player.gameObject);
			playerIdsDict.Remove(playerId);
			OnlineArena.Instance.SetSize ((uint) playerIdsDict.Count);
		}

		public virtual void OnMakeEventsMessage (object sender, Message message)
		{
			if (isPlaying)
				MakeEvents (message);
			else
			{
				for (uint i = 0; i < message.Count; i += DATA_PER_EVENT)
					makeEventMessages.Add(Message.Create("Make Events", message.GetUnsignedInteger(i), message.GetFloat(i + 1), message.GetFloat(i + 2), message.GetFloat(i + 3)));
			}
		}

		public virtual void OnEventDoneMessage (object sender, Message message)
		{
			int index = (int) message.GetUnsignedInteger(0);
			makeEventMessages.RemoveAt(index);
			events.RemoveAt(index);
		}

		public virtual void MakeEvents (Message message)
		{
			for (uint i = 0; i < message.Count; i += DATA_PER_EVENT)
			{
				uint eventTypeIndex = message.GetUnsignedInteger(i);
				Vector2 spawnPosition = new Vector2(message.GetFloat(i + 1), message.GetFloat(i + 2));
				float spawnRotation = message.GetFloat(i + 3);
				events.Add(MakeEvent (OnlineBattle.Instance.eventPrefabs[eventTypeIndex], spawnPosition, spawnRotation));
			}
		}

		public override void DoUpdate ()
		{
			CameraScript.Instance.trs.position = localPlayer.lengthVisualizerTrs.position.SetZ(CameraScript.Instance.trs.position.z);
			foreach (OnlinePlayer player in nonLocalPlayers)
				player.UpdateGraphics ();
		}

		public virtual void OnDestroy ()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			if (NetworkManager.connection != null)
			{
				NetworkManager.connection.OnMessage -= OnMessage;
				NetworkManager.connection.Disconnect();
			}
			if (NetworkManager.client != null)
				NetworkManager.client.Logout();
			spawnPlayerMessages.Clear();
			makeEventMessages.Clear();
			events.Clear();
			NetworkManager.IsOnline = false;
			isWaitingForAnotherPlayer = false;
			playerIdsDict.Clear();
		}
	}
}
