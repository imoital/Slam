using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerRowUI : MonoBehaviour
{
	public TMP_Text playerNameText;
	public Button leftButton;
	public Button rightButton;

	private PreLobby lobby;
	private Lobby.LobbyPlayerView player;

	public void Bind(PreLobby lobby, Lobby.LobbyPlayerView player)
	{
		this.lobby = lobby;
		this.player = player;

		if (playerNameText != null) {
			playerNameText.text = player.name;
		}

		BindButtons();
	}

	void BindButtons()
	{
		if (leftButton != null) {
			leftButton.onClick.RemoveAllListeners();
			leftButton.gameObject.SetActive(CanMoveLeft());
			if (CanMoveLeft()) {
				leftButton.onClick.AddListener(MoveLeft);
			}
		}

		if (rightButton != null) {
			rightButton.onClick.RemoveAllListeners();
			rightButton.gameObject.SetActive(CanMoveRight());
			if (CanMoveRight()) {
				rightButton.onClick.AddListener(MoveRight);
			}
		}
	}

	bool CanMoveLeft()
	{
		return lobby != null && lobby.IsLocalGame() && lobby.CanShowLobbyArrows() && (player.team == 2 || player.team == 0);
	}

	bool CanMoveRight()
	{
		return lobby != null && lobby.IsLocalGame() && lobby.CanShowLobbyArrows() && (player.team == 1 || player.team == 0);
	}

	void MoveLeft()
	{
		if (lobby == null) return;
		int newTeam = player.team == 0 ? 1 : 0;
		lobby.MoveLocalPlayer(player.controller, player.team, newTeam);
	}

	void MoveRight()
	{
		if (lobby == null) return;
		int newTeam = player.team == 0 ? 2 : 0;
		lobby.MoveLocalPlayer(player.controller, player.team, newTeam);
	}
}
