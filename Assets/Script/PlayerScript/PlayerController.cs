using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // LINQを使用するため追加
using UnityEngine.SceneManagement; // SceneManagerを使用するために追加

/// <summary>
/// プレイヤーの移動、エネルギー管理、攻撃、およびアーマー制御を制御します。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // 構造体/クラスの定義
    public enum WeaponMode { Melee, Beam }
    public enum ArmorMode { Normal = 0, Buster = 1, Speed = 2 } // 明示的に値を設定 (インデックスと対応)

    // PlayerPrefsのキー
    private const string SelectedArmorKey = "SelectedArmorIndex";

    // Scene Management
    [Header("Game Over Settings")]
    // private string gameOverSceneName = "GameOverScene"; // ★ 削除: 遷移先はSceneBasedGameOverManagerが管理
    [Tooltip("シーンベースのゲームオーバーマネージャーを設定")]
    public SceneBasedGameOverManager gameOverManager; // ★ 追加: マネージャーへの参照

    // アーマーのステータスを保持するクラス
    [System.Serializable]
    public class ArmorStats
    {
        public string name;
        [Tooltip("ダメージ軽減率 (例: 1.0 = 変更なし, 0.5 = ダメージ半減)")]
        public float defenseMultiplier = 1.0f;
        [Tooltip("移動速度補正 (例: 1.5 = 1.5倍速)")]
        public float moveSpeedMultiplier = 1.0f;
        [Tooltip("エネルギー回復補正")]
        public float energyRecoveryMultiplier = 1.0f;
    }

    // 依存オブジェクト - Awakeで確実に取得
    private CharacterController _controller;
    // TPSCameraControllerは未定義のため、引き続き警告を残しつつOptionalとして扱う
    private TPSCameraController _tpsCamController;

    //UI & Visuals 
    [Header("Armor UI & Visuals")]
    public Image currentArmorIconImage;
    [Tooltip("Normal(0), Buster(1), Speed(2) の順で設定")]
    public Sprite[] armorSprites;
    [Tooltip("Normal(0), Buster(1), Speed(2) の順で設定。CharacterControllerの子に配置したモデルGameObjectを設定してください。")]
    public GameObject[] armorModels;

    [Header("Weapon UI")]
    public Image meleeWeaponIcon;
    public Image beamWeaponIcon;
    public Color emphasizedColor = Color.white;
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f);

    // ベースとなる能力値 
    [Header("Base Stats")]
    public float baseMoveSpeed = 15.0f;
    public float boostMultiplier = 2.0f;
    public float verticalSpeed = 10.0f;
    public float energyConsumptionRate = 15.0f;
    public float energyRecoveryRate = 10.0f;
    public float meleeAttackRange = 2.0f;
    public float meleeDamage = 50.0f;
    public float beamDamage = 50.0f;
    public float beamAttackEnergyCost = 30.0f;
    public bool canFly = true;
    public float gravity = -9.81f;

    // ArmrModeのインデックスと一致させる - 値を修正
    [Header("Armor Settings")]
    public List<ArmorStats> armorConfigurations = new List<ArmorStats>
    {
        new ArmorStats { name = "Normal", defenseMultiplier = 1.0f, moveSpeedMultiplier = 1.0f, energyRecoveryMultiplier = 1.0f },
        //  Buster Mode: 防御力を犠牲にして攻撃特化 (ダメージ軽減率を1.5f (ダメージ1.5倍)に)
        new ArmorStats { name = "Buster Mode", defenseMultiplier = 1.5f, moveSpeedMultiplier = 0.8f, energyRecoveryMultiplier = 0.8f },
        //  Speed Mode: 防御力も高めに設定 (ダメージ軽減率を0.75f (ダメージ0.75倍)に)
        new ArmorStats { name = "Speed Mode", defenseMultiplier = 0.75f, moveSpeedMultiplier = 1.5f, energyRecoveryMultiplier = 1.2f }
    };

    // 内部状態をprivate fieldとpublic propertyに分離
    private ArmorMode _currentArmorMode = ArmorMode.Normal;
    private ArmorStats _currentArmorStats;
    private float _currentHP;
    private float _currentEnergy;
    private bool _isAttacking = false;
    private float _attackTimer = 0.0f;
    private WeaponMode _currentWeaponMode = WeaponMode.Melee;
    private float _lastEnergyConsumptionTime;
    private bool _hasTriggeredEnergyDepletedEvent = false;
    private bool _isDead = false; // 死亡状態を追跡するフラグ

    // 公開プロパティ (読み取り専用)
    [HideInInspector] public float currentHP { get => _currentHP; private set => _currentHP = value; }
    [HideInInspector] public float currentEnergy { get => _currentEnergy; private set => _currentEnergy = value; }
    public ArmorMode currentArmorMode => _currentArmorMode;
    public WeaponMode currentWeaponMode => _currentWeaponMode;

    // HP/Energy Gauge
    [Header("Health Settings")]
    public float maxHP = 100.0f;
    public Slider hPSlider;

    [Header("Energy Gauge Settings")]
    public float maxEnergy = 100.0f;
    public float recoveryDelay = 1.0f;
    public Slider energySlider;

    // Attack Settings
    public float attackFixedDuration = 0.8f;

    [Header("Beam VFX")]
    public BeamController beamPrefab;
    public Transform beamFirePoint;
    public float beamMaxDistance = 100f;

    [Header("Melee Attack Settings")]
    public GameObject hitEffectPrefab;
    public LayerMask enemyLayer;

    // チュートリアル用イベントとプロパティ
    public Action onMeleeAttackPerformed;
    public Action onBeamAttackPerformed;
    public event Action onEnergyDepleted;
    public float WASDMoveTimer { get; private set; }
    public float JumpTimer { get; private set; }
    public float DescendTimer { get; private set; }

    // 移動関連の内部変数
    private Vector3 _velocity;
    private float _moveSpeed; // 実行中の速度
    public bool canReceiveInput = true;

    // Awakeでコンポーネント取得を確実に
    void Awake()
    {
        InitializeComponents();
        // 初期ステータスの設定はStartで行う
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        currentHP = maxHP;

        LoadAndSwitchArmor();
        UpdateHPUI();
        UpdateEnergyUI();
        UpdateWeaponUIEmphasis();

        // SceneBasedGameOverManagerがInspectorで設定されていない場合、シーンから取得を試みる
        if (gameOverManager == null)
        {
            gameOverManager = FindObjectOfType<SceneBasedGameOverManager>();
            if (gameOverManager == null)
            {
                Debug.LogWarning("SceneBasedGameOverManagerがInspectorで設定されていません。シーンから取得もできませんでした。Die()時にエラーが発生する可能性があります。");
            }
        }

        Debug.Log($"初期武器: {currentWeaponMode} | 初期アーマー: {currentArmorMode}");
    }

    /// <summary>コンポーネントの初期化とエラーチェック</summary>
    private void InitializeComponents()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            Debug.LogError($"{nameof(PlayerController)}: CharacterControllerが見つかりません。");
            enabled = false;
            return;
        }

        // ※ TPSCameraControllerが未定義の場合、この行はエラーになる可能性があります。
        _tpsCamController = FindObjectOfType<TPSCameraController>();
        if (_tpsCamController == null)
        {
            Debug.LogWarning($"{nameof(PlayerController)}: TPSCameraControllerが見つかりません。");
        }
    }

    /// <summary>PlayerPrefsから保存されたアーマーインデックスを読み込み、反映させる</summary>
    private void LoadAndSwitchArmor()
    {
        int selectedIndex = PlayerPrefs.GetInt(SelectedArmorKey, (int)ArmorMode.Normal);

        // 有効なEnum値か、また設定リストの範囲内かチェック
        if (Enum.IsDefined(typeof(ArmorMode), selectedIndex) && selectedIndex < armorConfigurations.Count)
        {
            ArmorMode initialMode = (ArmorMode)selectedIndex;
            SwitchArmor(initialMode, false); // ロード時はログを出さない
        }
        else
        {
            SwitchArmor(ArmorMode.Normal, false);
            Debug.LogWarning($"不正なアーマーインデックス({selectedIndex})が検出されました。Normalモードを適用します。");
        }
    }

    void Update()
    {
        // PキーでHPを0にするテスト
        HandleTestInput();

        // 死亡状態の場合は入力をすべてブロック
        if (_isDead) return;

        // 攻撃中または入力無効化中は移動・攻撃入力をブロック
        if (!canReceiveInput || _isAttacking)
        {
            HandleAttackState();
            WASDMoveTimer = JumpTimer = DescendTimer = 0f;
            _controller.Move(Vector3.up * _velocity.y * Time.deltaTime); // 垂直移動のみ継続
        }
        else // 攻撃中でない場合
        {
            // カメラ方向への回転
            _tpsCamController?.RotatePlayerToCameraDirection();

            HandleAttackInputs();
            HandleWeaponSwitchInput(); // Eキー
            HandleArmorSwitchInput(); // 1, 2, 3キー

            HandleEnergy();

            // 処理順序を整理
            Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
            _controller.Move(finalMove * Time.deltaTime);
        }
    }

    /// <summary>PキーでHPを0にするテスト</summary>
    private void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.LogWarning("Pキーが押されました: HPを0にして死亡処理を実行します。");
            currentHP = 0;
            UpdateHPUI(); // UIを更新
            Die();       // 死亡処理を呼び出す
        }
    }

    /// <summary>1, 2, 3キーでのアーマー切り替えを処理します。</summary>
    private void HandleArmorSwitchInput()
    {
        // 最適化: ArmorModeのインデックスを利用して、汎用的に処理
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchArmor(ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchArmor(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchArmor(ArmorMode.Speed);
    }

    /// <summary>指定されたアーマーモードに切り替え、ステータスを更新します。</summary>
    private void SwitchArmor(ArmorMode newMode, bool shouldLog = true)
    {
        int index = (int)newMode;
        if (index < 0 || index >= armorConfigurations.Count)
        {
            Debug.LogError($"アーマーモード {newMode} の設定が見つかりません。");
            return;
        }

        if (_currentArmorMode == newMode && _currentArmorStats != null)
        {
            if (shouldLog) Debug.Log($"アーマーは既に **{newMode}** です。");
            return;
        }

        _currentArmorMode = newMode;
        _currentArmorStats = armorConfigurations[index];

        // ステータスへの適用
        // 1. 移動速度の更新 (baseMoveSpeed -> _moveSpeed)
        _moveSpeed = baseMoveSpeed * _currentArmorStats.moveSpeedMultiplier;

        // PlayerPrefsに選択されたインデックスを保存
        PlayerPrefs.SetInt(SelectedArmorKey, index);
        PlayerPrefs.Save(); // 書き込みを保証

        // 視覚的要素の更新
        UpdateArmorVisuals(index);

        if (shouldLog)
        {
            Debug.Log($"アーマーを切り替えました: **{_currentArmorStats.name}** " +
                        $" (速度補正: x{_currentArmorStats.moveSpeedMultiplier}, 防御補正: x{_currentArmorStats.defenseMultiplier}, 回復補正: x{_currentArmorStats.energyRecoveryMultiplier})");
        }
    }

    /// <summary>アーマーモードの視覚的な要素（UIアイコンとモデル）を更新します。 🎨</summary>
    private void UpdateArmorVisuals(int index)
    {
        // 1. UIアイコンの更新
        if (currentArmorIconImage != null && armorSprites != null && index < armorSprites.Length)
        {
            currentArmorIconImage.sprite = armorSprites[index];
            currentArmorIconImage.enabled = true;
        }

        // 2. プレイヤーモデル（GameObject）の更新
        if (armorModels != null && armorModels.Length > 0)
        {
            // NullチェックをforEachで抽象化
            for (int i = 0; i < armorModels.Length; i++)
            {
                if (armorModels[i] != null)
                {
                    // 現在のインデックスと一致するモデルのみを有効化し、他を無効化
                    armorModels[i].SetActive(i == index);
                }
            }
        }
        // WarningはSwitchArmor関数で一度にログを出すようにする方がノイズが少ない
    }

    /// <summary>Eキーでの武器切り替えを処理します。</summary>
    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchWeapon();
        }
    }

    /// <summary>武器モードを切り替えます。</summary>
    private void SwitchWeapon()
    {
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Melee) ? WeaponMode.Beam : WeaponMode.Melee;

        Debug.Log($"武器を切り替えました: **{_currentWeaponMode}**");
        UpdateWeaponUIEmphasis();
    }

    /// <summary>現在の武器モードに応じてUIアイコンを強調表示します。</summary>
    private void UpdateWeaponUIEmphasis()
    {
        if (meleeWeaponIcon == null || beamWeaponIcon == null)
        {
            // Debug.LogWarning("武器アイコンのImageコンポーネントが設定されていません。Inspectorを確認してください。");
            return;
        }

        bool isMelee = (_currentWeaponMode == WeaponMode.Melee);

        // 三項演算子で簡略化
        meleeWeaponIcon.color = isMelee ? emphasizedColor : normalColor;
        beamWeaponIcon.color = isMelee ? normalColor : emphasizedColor;
    }

    /// <summary>水平方向の移動処理</summary>
    private Vector3 HandleHorizontalMovement()
    {
        if (_isAttacking) return Vector3.zero;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 入力がない場合は早期リターン
        if (h == 0f && v == 0f)
        {
            WASDMoveTimer = 0f;
            return Vector3.zero;
        }

        Vector3 inputDirection = new Vector3(h, 0, v);
        Vector3 moveDirection;

        // カメラ基準の移動
        if (_tpsCamController != null)
        {
            // カメラの回転を水平軸のみに適用
            Quaternion cameraRotation = Quaternion.Euler(0, _tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraRotation * inputDirection;
        }
        else
        {
            moveDirection = transform.right * h + transform.forward * v;
        }

        moveDirection.Normalize();

        float currentSpeed = _moveSpeed; // アーマー補正済みの_moveSpeedを使用
        bool isConsumingEnergy = false;

        // ブースト処理 (Ctrlキー)
        bool isBoosting = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && currentEnergy > 0.01f;

        if (isBoosting)
        {
            currentSpeed *= boostMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime; // set accessorで_currentEnergyが更新される
            isConsumingEnergy = true;
        }

        // エネルギー枯渇時の速度制限
        // if (currentEnergy <= 0.01f) { currentSpeed = _moveSpeed; } // 既にブースト判定で制限されているため不要

        Vector3 horizontalMove = moveDirection * currentSpeed;

        // チュートリアル用タイマー更新
        WASDMoveTimer += Time.deltaTime;

        if (isConsumingEnergy) _lastEnergyConsumptionTime = Time.time;

        return horizontalMove;
    }

    /// <summary>垂直方向の移動処理と重力適用</summary>
    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = _controller.isGrounded;
        if (isGrounded && _velocity.y < 0) _velocity.y = -0.1f;

        bool isConsumingEnergy = false;
        bool hasVerticalInput = false;

        // 上昇/下降の入力処理
        if (canFly && currentEnergy > 0.01f && !_isAttacking)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                _velocity.y = verticalSpeed;
                JumpTimer += Time.deltaTime;
                DescendTimer = 0f;
                hasVerticalInput = isConsumingEnergy = true;
            }
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                _velocity.y = -verticalSpeed;
                DescendTimer += Time.deltaTime;
                JumpTimer = 0f;
                hasVerticalInput = isConsumingEnergy = true;
            }
        }

        // 入力がない、またはエネルギーが枯渇した場合
        if (!hasVerticalInput)
        {
            // キーアップ時のタイマーリセットは不要（Update内で継続的にJumpTimer=0fで上書きされるため）

            // 重力適用
            if (!isGrounded && !_isAttacking) // 攻撃中は垂直速度を固定する方が自然
            {
                _velocity.y += gravity * Time.deltaTime;
            }
        }
        else
        {
            // エネルギー消費
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            _lastEnergyConsumptionTime = Time.time;
        }

        // エネルギー枯渇時の垂直方向の制御を停止 (急降下へ)
        if (currentEnergy <= 0.01f && _velocity.y > 0)
        {
            _velocity.y = 0;
            JumpTimer = DescendTimer = 0f;
        }

        return new Vector3(0, _velocity.y, 0);
    }

    /// <summary>攻撃入力の処理</summary>
    private void HandleAttackInputs()
    {
        if (Input.GetMouseButtonDown(0) && !_isAttacking)
        {
            switch (_currentWeaponMode)
            {
                case WeaponMode.Melee:
                    HandleMeleeAttack();
                    break;
                case WeaponMode.Beam:
                    HandleBeamAttack();
                    break;
            }
        }
    }

    /// <summary>近接攻撃を実行</summary>
    private void HandleMeleeAttack()
    {
        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f; // 攻撃中は垂直方向を固定

        // Physics.OverlapSphereのコールはコストが高いため、ログは本番では削除/コメントアウトを推奨
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeAttackRange, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == this.transform) continue;

            // 敵のダメージ処理コンポーネントを取得 (例: 'IDamageable' インターフェース)
            // if (hitCollider.TryGetComponent<EnemyHealth>(out var enemyHealth)) {
            //    enemyHealth.TakeDamage(meleeDamage);
            //    // 2. ヒットエフェクトを生成
            //    if (hitEffectPrefab != null) {
            //        Instantiate(hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
            //    }
            // }

            // デバッグ用: EnemyHealthがなくてもエフェクトを生成するロジックを維持
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
            }
        }

        onMeleeAttackPerformed?.Invoke();
    }

    /// <summary>ビーム攻撃を実行</summary>
    private void HandleBeamAttack()
    {
        if (currentEnergy < beamAttackEnergyCost)
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        if (beamFirePoint == null || beamPrefab == null)
        {
            Debug.LogError("ビームの発射点(BeamFirePoint)またはビームのプレハブ(BeamPrefab)が設定されていません。");
            return;
        }

        _isAttacking = true;
        _attackTimer = 0f;
        _velocity.y = 0f;

        // エネルギー消費
        currentEnergy -= beamAttackEnergyCost;
        _lastEnergyConsumptionTime = Time.time;
        UpdateEnergyUI();

        // Raycastで着弾点を計算する
        Vector3 origin = beamFirePoint.position;
        // カメラの向きを使う方が自然だが、ここではbeamFirePointの前方を使用
        Vector3 direction = beamFirePoint.forward;

        RaycastHit hit;
        Vector3 endPoint;
        bool didHit = false;

        // Raycastでヒット確認 (すべてのレイヤーに当たるようにします)
        if (Physics.Raycast(origin, direction, out hit, beamMaxDistance, ~0))
        {
            endPoint = hit.point;
            didHit = true;

            // 敵にダメージを与える（必要に応じて）
            // if (hit.collider.TryGetComponent<EnemyHealth>(out var enemyHealth)) {
            //    enemyHealth.TakeDamage(beamDamage);
            // }
        }
        else
        {
            endPoint = origin + direction * beamMaxDistance;
        }

        // BeamControllerを生成し、Fireメソッドを呼び出す
        BeamController beamInstance = Instantiate(
            beamPrefab,
            origin,
            beamFirePoint.rotation
        );
        beamInstance.Fire(origin, endPoint, didHit);

        onBeamAttackPerformed?.Invoke();
    }

    /// <summary>攻撃中のプレイヤーの状態を処理（移動ロックなど）</summary>
    void HandleAttackState()
    {
        if (!_isAttacking) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackFixedDuration)
        {
            _isAttacking = false;
            _attackTimer = 0.0f;

            // 攻撃終了時: 接地していなければ重力の影響を受け始める
            if (!_controller.isGrounded)
            {
                _velocity.y = 0; // 急降下を防ぐ
            }
            else
            {
                _velocity.y = -0.1f; // 接地判定を維持
            }
        }
    }

    /// <summary>エネルギー回復と枯渇イベントの処理</summary>
    private void HandleEnergy()
    {
        // エネルギー回復
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            // _currentArmorStatsのNullチェックを不要にするため、Startで必ず初期化されるように保証
            float recoveryMultiplier = _currentArmorStats != null ? _currentArmorStats.energyRecoveryMultiplier : 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            currentEnergy += recoveryRate * Time.deltaTime;
        }

        // 値をクランプし、UIを更新
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();

        // エネルギー枯渇イベントの発火とフラグの管理
        if (currentEnergy <= 0.1f && !_hasTriggeredEnergyDepletedEvent)
        {
            onEnergyDepleted?.Invoke();
            _hasTriggeredEnergyDepletedEvent = true;
        }
        else if (currentEnergy > 0.1f && _hasTriggeredEnergyDepletedEvent && Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            _hasTriggeredEnergyDepletedEvent = false;
        }
    }

    // チュートリアル・UI関連のメソッド

    /// <summary>チュートリアル用の入力追跡フラグとタイマーをリセットする。</summary>
    public void ResetInputTracking()
    {
        WASDMoveTimer = JumpTimer = DescendTimer = 0f;
    }

    /// <summary>エネルギーゲージを更新する。UI更新は専用メソッドに集約</summary>
    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    /// <summary>HPスライダーを更新する。UI更新は専用メソッドに集約</summary>
    void UpdateHPUI()
    {
        if (hPSlider != null)
        {
            hPSlider.value = currentHP / maxHP;
        }
    }

    /// <summary>外部からダメージを受けたときに呼び出されます。</summary>
    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return; // 死亡状態なら処理しない

        float finalDamage = damageAmount;

        if (_currentArmorStats != null)
        {
            // アーマーの防御補正を適用したダメージ計算 (値が小さいほどダメージ軽減)
            finalDamage *= _currentArmorStats.defenseMultiplier;
        }

        currentHP -= finalDamage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateHPUI();

        Debug.Log($"ダメージを受けました。残りHP: {currentHP} (元のダメージ: {damageAmount}, 最終ダメージ: {finalDamage})");

        if (currentHP <= 0)
        {
            Die(); // HPが0になったら死亡処理を呼び出す
        }
    }

    /// <summary>プレイヤーの死亡処理とシーン移行</summary>
    private void Die()
    {
        if (_isDead) return; // 二重に死亡処理を呼ばない

        _isDead = true;
        canReceiveInput = false; // 入力を無効化

        Debug.Log("プレイヤーは破壊されました。ゲームオーバー処理をマネージャーに委譲します。");

        // オブジェクトの非表示、アニメーション再生、エフェクト表示などを行う

        // シーン遷移ロジックをSceneBasedGameOverManagerに委譲
        if (gameOverManager != null)
        {
            gameOverManager.GoToGameOverScene(); // ★ 適切なシーンへの遷移をマネージャーに依頼
        }
        else
        {
            Debug.LogError("SceneBasedGameOverManagerが設定されていません。Inspectorを確認してください。");
        }

        // PlayerController自体を無効化（シーン移行が失敗した場合のフォールバック）
        enabled = false;
    }

    // OnDrawGizmosSelectedは変更なし (意図されたデバッグ機能のため)
    private void OnDrawGizmosSelected()
    {
        // 1. 近接攻撃の範囲 (球体)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, meleeAttackRange);

        // 2. ビーム攻撃の射程
        if (beamFirePoint != null)
        {
            Vector3 origin = beamFirePoint.position;
            Vector3 direction = beamFirePoint.forward;
            RaycastHit hit;
            Vector3 endPoint;

            if (Physics.Raycast(origin, direction, out hit, beamMaxDistance, ~0))
            {
                Gizmos.color = Color.red;
                endPoint = hit.point;
                Gizmos.DrawSphere(endPoint, 0.1f);
            }
            else
            {
                Gizmos.color = Color.cyan;
                endPoint = origin + direction * beamMaxDistance;
            }
            Gizmos.DrawLine(origin, endPoint);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin + direction * beamMaxDistance, 0.05f);
        }
    }
}