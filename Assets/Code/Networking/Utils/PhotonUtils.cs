using System;
using UnityEngine;

namespace Assets.Code.Networking.Utils
{
    public class PhotonUtils
    {
        public static PhotonPlayer GetPlayerById(int id)
        {
            return PhotonPlayer.Find(id);
        }

        public static int[] GetPlayerIdsInRoom()
        {
            var ids = new int[PhotonNetwork.otherPlayers.Length];

            for (int i = 0; i < PhotonNetwork.otherPlayers.Length; i++)
            {
                ids[i] = PhotonNetwork.otherPlayers[i].ID;
            }

            return ids;
        }

        public static Transform GetPhotonPlayerTransform(PhotonPlayer player)
        {
            foreach (var go in GameObject.FindGameObjectsWithTag("NetworkPlayer"))
            {
                if (go.GetComponent<PhotonView>().ownerId == player.ID)
                    return go.transform;
            }

            return null;
        }

        public static Int32 GetUnixTime()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}