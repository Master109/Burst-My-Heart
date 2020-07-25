using UnityEngine;
using Extensions;

namespace BMH
{
	// [ExecuteInEditMode]
	public class ObjectInWorld : MonoBehaviour
	{
		public Transform trs;
		public GameObject go;
		public Transform duplicateTrs;
		public GameObject duplicateGo;
		public ObjectInWorld duplicateWorldObject;
		public bool IsInPieces
		{
			get
			{
				return pieceIAmIn != null;
			}
		}
		public WorldPiece pieceIAmIn;

// #if UNITY_EDITOR
// 		public virtual void OnEnable ()
// 		{
// 			if (Application.isPlaying)
// 				return;
// 			if (trs == null)
// 				trs = GetComponent<Transform>();
// 			if (go == null)
// 				go = gameObject;
// 			if (duplicateTrs == null)
// 			{
// 				GameObject gameSceneRootGo = GameObject.Find("Game Scene");
// 				if (gameSceneRootGo != null)
// 				{
// 					Transform gameSceneRoot = gameSceneRootGo.GetComponent<Transform>();
// 					if (gameSceneRoot != null)
// 					{
// 						foreach (Transform child in gameSceneRoot.FindChildren(name))
// 						{
// 							ObjectInWorld objectInWorld = child.GetComponent<ObjectInWorld>();
// 							if (objectInWorld != null && objectInWorld.IsInPieces != IsInPieces && child.position == trs.position)
// 							{
// 								objectInWorld.duplicateTrs = trs;
// 								objectInWorld.duplicateGo = go;
// 								duplicateTrs = objectInWorld.trs;
// 								duplicateGo = objectInWorld.go;
// 							}
// 						}
// 					}
// 				}
// 			}
// 		}
// #endif

		public virtual void OnDisable ()
		{
		}
	}
}