using UnityEngine;

public class CameraControl : MonoBehaviour
{
	public float m_DampTime = 0.2f;                 // カメラが目標に到達するまでの時間
	public float m_ScreenEdgeBuffer = 4f;           // 戦車が画面端に貼り付かないようにするために設定する余地
	public float m_MinSize = 6.5f;                  // カメラが画面に寄り過ぎないようにするために設定する最小値
	[HideInInspector] public Transform[] m_Targets; // カメラにおさめる対象（戦車）を配列で格納する


	private Camera m_Camera;                        // カメラ
	private float m_ZoomSpeed;                      // カメラのズーム速度
	private Vector3 m_MoveVelocity;                 // カメラの移動速度
	private Vector3 m_DesiredPosition;              // カメラが向かっている位置


	private void Awake ()
	{
		m_Camera = GetComponentInChildren<Camera> ();
	}


	private void FixedUpdate ()
	{
		Move ();
		Zoom ();
	}


	private void Move ()
	{
		// 2点間の中央地点を求める
		FindAveragePosition ();

		// 目的座標にカメラリグを移動させる
		transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition, ref m_MoveVelocity, m_DampTime);
	}


	private void FindAveragePosition ()
	{
		Vector3 averagePos = new Vector3 ();
		int numTargets = 0;

		// 残存している全ての戦車の情報を確認し、位置情報を加えていく
		for (int i = 0; i < m_Targets.Length; i++)
		{
			// プレイヤーが死亡している時は戦車が画面内に存在しないためスキップする
			if (!m_Targets[i].gameObject.activeSelf)
				continue;

			// ベクトル変数に戦車の位置情報を加える
			averagePos += m_Targets[i].position;
			numTargets++;
		}

		// 戦車が複数台残っている場合は、中央地点を求める
		if (numTargets > 0)
			averagePos /= numTargets;

		// 戦車やカメラはY座標を移動しないため安全装置として初期値0を維持させる
		averagePos.y = transform.position.y;

		// メンバ変数に目標地点を格納する
		m_DesiredPosition = averagePos;
	}


	private void Zoom ()
	{
		// 複数台の戦車が画面内におさまるためのカメラサイズを求め変更する
		float requiredSize = FindRequiredSize();
		m_Camera.orthographicSize = Mathf.SmoothDamp (m_Camera.orthographicSize, requiredSize, ref m_ZoomSpeed, m_DampTime);
	}


	private float FindRequiredSize ()
	{
		// カメラリグのローカル空間において、カメラの目標地点の座標を取得する
		Vector3 desiredLocalPos = transform.InverseTransformPoint(m_DesiredPosition);

		// カメラサイズの初期値
		float size = 0f;

		// 残存している全ての戦車の情報を確認し、位置情報を加えていく
		// 最も離れた場所にある戦車を含んでズームアウトすれば画面内におさまる
		for (int i = 0; i < m_Targets.Length; i++)
		{
			if (!m_Targets[i].gameObject.activeSelf)
				continue;

			// カメラリグのローカル空間における戦車の座標を取得する
			Vector3 targetLocalPos = transform.InverseTransformPoint(m_Targets[i].position);

			// カメラリグのローカル空間において、カメラ移動後の目標地点を原点として戦車の距離を求める
			Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;

			// 現在のカメラサイズとカメラリグのローカル空間における戦車のY軸の距離を比較し大きい方を格納する
			size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));

			// 現在のカメラサイズとカメラリグのローカル空間における戦車のX軸の距離を比較し大きい方を格納する
			size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / m_Camera.aspect);
		}

		// カメラの画面端に戦車が画面端に貼り付かないようにするため余地（遊び）を加える
		size += m_ScreenEdgeBuffer;

		// カメラが画面に寄り過ぎないようにするため、カメラサイズの最小値と比較し下回っていたら上書きする
		size = Mathf.Max (size, m_MinSize);

		return size;
	}

	// GameManagerクラスからゲーム開始時に呼び出される
	public void SetStartPositionAndSize ()
	{
		FindAveragePosition ();
		transform.position = m_DesiredPosition;
		m_Camera.orthographicSize = FindRequiredSize ();
	}
}