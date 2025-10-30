using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// プレイヤーの移動、エネルギー管理、攻撃、およびアーマー制御を制御します。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ★追加: 武器モードとアーマーモードの定義
    public enum WeaponMode { Melee, Beam }
    public enum ArmorMode { Normal, Defense, Speed }

    // ★追加: アーマーのステータスを保持するクラス
    [System.Serializable]
    public class ArmorStats
    {
        public string name;
        public float defenseMultiplier = 1.0f; // ダメージ軽減率 (例: 1.0 = 変更なし, 0.5 = ダメージ半減)
        public float moveSpeedMultiplier = 1.0f; // 移動速度補正 (例: 1.5 = 1.5倍速)
        public float energyRecoveryMultiplier = 1.0f; // エネルギー回復補正
    }

    // 依存オブジェクト
    private CharacterController controller;
    private TPSCameraController tpsCamController;

    // ★追加: UIアイコンの参照
    [Header("Weapon UI")]
    public Image meleeWeaponIcon; // 近接武器アイコンのImageコンポーネント
    public Image beamWeaponIcon;  // ビーム武器アイコンのImageコンポーネント
    public Color emphasizedColor = Color.white; // 強調時の色 (例: 白や明るい色)
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f); // 通常時の色 (例: グレー)


    // --- ベースとなる能力値 ---
    [Header("Base Stats")]
    public float baseMoveSpeed = 15.0f; // ★変更: ベースの値を保持
    public float moveSpeed = 15.0f; // 実行中の速度
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

    // ★追加: アーマー設定
    [Header("Armor Settings")]
    public List<ArmorStats> armorConfigurations = new List<ArmorStats>
    {
        new ArmorStats { name = "Normal", defenseMultiplier = 1.0f, moveSpeedMultiplier = 1.0f, energyRecoveryMultiplier = 1.0f },
        new ArmorStats { name = "Defense Mode", defenseMultiplier = 0.5f, moveSpeedMultiplier = 0.8f, energyRecoveryMultiplier = 0.8f },
        new ArmorStats { name = "Speed Mode", defenseMultiplier = 1.2f, moveSpeedMultiplier = 1.5f, energyRecoveryMultiplier = 1.2f }
    };
    private ArmorMode _currentArmorMode = ArmorMode.Normal;
    private ArmorStats _currentArmorStats;


    //[HPゲージ関連の追加]
    [Header("Health Settings")]
    public float maxHP = 100.0f; // 最大HP
    [HideInInspector] public float currentHP; // 現在HP
    public Slider hPSlider; // HPスライダー (UI)

    //エネルギーゲージ関連
    [Header("Energy Gauge Settings")]
    public float maxEnergy = 100.0f;
    [HideInInspector] public float currentEnergy;
    public float recoveryDelay = 1.0f;
    private float lastEnergyConsumptionTime;
    public Slider energySlider;
    private bool hasTriggeredEnergyDepletedEvent = false;

    // 内部状態と移動関連
    private Vector3 velocity;
    private bool isAttacking = false;
    private float attackTimer = 0.0f;
    public float attackFixedDuration = 0.8f;

    // ★武器モード
    private WeaponMode _currentWeaponMode = WeaponMode.Melee;

    // プレイヤー入力制御
    public bool canReceiveInput = true;

    [Header("Beam VFX")]
    public BeamController beamPrefab; // 作成したBeamControllerを持つプレハブ
    public Transform beamFirePoint; // ビームの発射元となるTransform (例: プレイヤーの手や銃口)
    public float beamMaxDistance = 100f; // ビームの最大到達距離

    // チュートリアル用イベントとプロパティ
    public Action onMeleeAttackPerformed;
    public Action onBeamAttackPerformed;
    public event Action onEnergyDepleted;

    // 自動実装プロパティ
    public float WASDMoveTimer { get; private set; }
    public float JumpTimer { get; private set; }
    public float DescendTimer { get; private set; }


    void Start()
    {
        InitializeComponents();
        currentEnergy = maxEnergy;
        UpdateEnergyUI();
        currentHP = maxHP; // 現在HPを最大HPで初期化
        UpdateHPUI();
        Debug.Log($"初期武器: {_currentWeaponMode}");

        // ★追加: 初期アーマーを設定
        SwitchArmor(ArmorMode.Normal);

        // ★追加: 初期武器のアイコンを強調表示
        UpdateWeaponUIEmphasis();
    }

    /// <summary>コンポーネントの初期化とエラーチェック</summary>
    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController: CharacterControllerが見つかりません。");
            enabled = false;
            return;
        }

        tpsCamController = FindObjectOfType<TPSCameraController>();
        if (tpsCamController == null)
        {
            Debug.LogWarning("PlayerController: TPSCameraControllerが見つかりません。");
        }
    }

    void Update()
    {
        // 攻撃中または入力無効化中は移動・攻撃入力をブロック
        if (!canReceiveInput || isAttacking)
        {
            HandleAttackState();
            WASDMoveTimer = JumpTimer = DescendTimer = 0f;
        }
        else // 攻撃中でない場合
        {
            // ロックオン機能がないため、常時カメラ方向に回転
            tpsCamController?.RotatePlayerToCameraDirection();

            HandleAttackInputs();
            HandleWeaponSwitchInput(); // Eキー

            // ★追加: 1, 2, 3キーでのアーマー変更を処理
            HandleArmorSwitchInput();
        }

        HandleEnergy();

        Vector3 finalMove = HandleVerticalMovement() + HandleHorizontalMovement();
        controller.Move(finalMove * Time.deltaTime);
    }

    /// <summary>1, 2, 3キーでのアーマー切り替えを処理します。</summary>
    private void HandleArmorSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchArmor(ArmorMode.Normal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchArmor(ArmorMode.Defense);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchArmor(ArmorMode.Speed);
        }
    }

    /// <summary>指定されたアーマーモードに切り替え、ステータスを更新します。</summary>
    private void SwitchArmor(ArmorMode newMode)
    {
        int index = (int)newMode;
        if (index < 0 || index >= armorConfigurations.Count)
        {
            Debug.LogError($"アーマーモード {newMode} の設定が見つかりません。");
            return;
        }

        _currentArmorMode = newMode;
        _currentArmorStats = armorConfigurations[index];

        // ステータスへの適用
        moveSpeed = baseMoveSpeed * _currentArmorStats.moveSpeedMultiplier;

        Debug.Log($"アーマーを切り替えました: **{_currentArmorStats.name}** " +
                  $" (速度補正: x{_currentArmorStats.moveSpeedMultiplier}, 防御補正: x{_currentArmorStats.defenseMultiplier})");
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
        if (_currentWeaponMode == WeaponMode.Melee)
        {
            _currentWeaponMode = WeaponMode.Beam;
        }
        else
        {
            _currentWeaponMode = WeaponMode.Melee;
        }
        Debug.Log($"武器を切り替えました: **{_currentWeaponMode}**");

        // ★追加: UIの強調表示を更新
        UpdateWeaponUIEmphasis();
    }

    // ------------------------------------------------------------------
    // ★追加: UIの強調表示ロジック
    // ------------------------------------------------------------------
    /// <summary>現在の武器モードに応じてUIアイコンを強調表示します。</summary>
    private void UpdateWeaponUIEmphasis()
    {
        if (meleeWeaponIcon == null || beamWeaponIcon == null)
        {
            Debug.LogWarning("武器アイコンのImageコンポーネントが設定されていません。Inspectorを確認してください。");
            return;
        }

        if (_currentWeaponMode == WeaponMode.Melee)
        {
            // 近接武器を強調
            meleeWeaponIcon.color = emphasizedColor;
            // ビーム武器を通常色に
            beamWeaponIcon.color = normalColor;
        }
        else // WeaponMode.Beam
        {
            // ビーム武器を強調
            beamWeaponIcon.color = emphasizedColor;
            // 近接武器を通常色に
            meleeWeaponIcon.color = normalColor;
        }
    }
    // ------------------------------------------------------------------


    /// <summary>水平方向の移動処理</summary>
    private Vector3 HandleHorizontalMovement()
    {
        // 攻撃中は水平移動を停止
        if (isAttacking) return Vector3.zero;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 moveDirection;
        // ロックオン機能がないため、常にカメラ基準の移動
        moveDirection = tpsCamController != null
            ? tpsCamController.transform.rotation * new Vector3(h, 0, v)
            : transform.right * h + transform.forward * v;

        moveDirection.y = 0;
        moveDirection.Normalize();

        float currentSpeed = moveSpeed; // ★アーマー補正済みのmoveSpeedを使用

        // ブースト処理 (Ctrlキー)
        bool isBoosting = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && currentEnergy > 0;
        bool isConsumingEnergy = false;

        if (isBoosting)
        {
            currentSpeed *= boostMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }

        // エネルギー枯渇時の速度制限
        if (currentEnergy <= 0.01f)
        {
            currentSpeed = moveSpeed;
        }

        Vector3 horizontalMove = moveDirection * currentSpeed;

        // チュートリアル用タイマー更新
        WASDMoveTimer = horizontalMove.magnitude > baseMoveSpeed * 0.1f ? WASDMoveTimer + Time.deltaTime : 0f;

        if (isConsumingEnergy) lastEnergyConsumptionTime = Time.time;

        return horizontalMove;
    }

    /// <summary>垂直方向の移動処理と重力適用</summary>
    private Vector3 HandleVerticalMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -0.1f;

        bool isConsumingEnergy = false;
        bool hasVerticalInput = false;

        if (canFly && currentEnergy > 0.01f)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                velocity.y = verticalSpeed;
                JumpTimer += Time.deltaTime;
                DescendTimer = 0f;
                hasVerticalInput = isConsumingEnergy = true;
            }
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                velocity.y = -verticalSpeed;
                DescendTimer += Time.deltaTime;
                JumpTimer = 0f;
                hasVerticalInput = isConsumingEnergy = true;
            }
        }

        // 入力がない、またはエネルギーが枯渇した場合の垂直移動の処理
        if (!hasVerticalInput)
        {
            // 上昇/下降入力がない場合はタイマーリセット
            if (Input.GetKeyUp(KeyCode.Space)) JumpTimer = 0f;
            if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt)) DescendTimer = 0f;

            // 重力適用
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        // エネルギー消費
        if (isConsumingEnergy)
        {
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            lastEnergyConsumptionTime = Time.time;
        }

        // エネルギー枯渇時の垂直方向の制御を停止
        if (currentEnergy <= 0.01f && velocity.y > 0)
        {
            velocity.y = 0;
            JumpTimer = DescendTimer = 0f;
        }

        return new Vector3(0, velocity.y, 0);
    }

    /// <summary>攻撃入力の処理</summary>
    private void HandleAttackInputs()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
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

    /// <summary>近接攻撃（デバッグ用）を実行</summary>
    private void HandleMeleeAttack()
    {
        isAttacking = true;
        attackTimer = 0f;

        velocity.y = 0f; // 攻撃中の垂直移動を停止

        // デバッグログ
        Debug.Log("近接攻撃 (Melee Attack) を実行: " + meleeDamage + " ダメージ");

        onMeleeAttackPerformed?.Invoke();
    }

    /// <summary>ビーム攻撃（デバッグ用）を実行</summary>
    private void HandleBeamAttack()
    {
        if (currentEnergy < beamAttackEnergyCost)
        {
            Debug.LogWarning("ビーム攻撃に必要なエネルギーがありません！");
            return;
        }

        // 攻撃状態へ移行... (既存のコード)
        isAttacking = true;
        attackTimer = 0f;
        velocity.y = 0f;

        // エネルギー消費... (既存のコード)
        currentEnergy -= beamAttackEnergyCost;
        lastEnergyConsumptionTime = Time.time;
        UpdateEnergyUI();

        // ===============================================
        // ★追加: ビームエフェクトのロジック
        // ===============================================

        // 1. Raycastで着弾点を計算する
        Vector3 origin = beamFirePoint.position;
        Vector3 direction = beamFirePoint.forward; // カメラの向きなど、適切な方向を設定

        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(origin, direction, out hit, beamMaxDistance))
        {
            // 何かに当たった場合
            endPoint = hit.point;
            // ※ ここで着弾エフェクト（Impact Particle System）を生成・再生する
        }
        else
        {
            // 何にも当たらなかった場合
            endPoint = origin + direction * beamMaxDistance;
        }

        // 2. BeamControllerを生成し、発射処理を呼び出す
        if (beamPrefab != null)
        {
            // beamPrefabをインスタンス化し、プレイヤーのTransformの子に設定
            BeamController beamInstance = Instantiate(beamPrefab, transform);

            // ビーム発射！
            beamInstance.Fire(origin, endPoint);

            // ※ ここで発射口エフェクト（Muzzle Flash Particle System）を再生する
            // MuzzleFlash.Play();
        }
        // ===============================================

        Debug.Log("ビーム攻撃 (Beam Attack) を実行: ...");
        onBeamAttackPerformed?.Invoke();
    }

    /// <summary>エネルギー回復と枯渇イベントの処理</summary>
    private void HandleEnergy()
    {
        // エネルギー回復
        // ★変更: アーマーの回復補正を適用
        if (Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            float recoveryRate = energyRecoveryRate * (_currentArmorStats != null ? _currentArmorStats.energyRecoveryMultiplier : 1.0f);
            currentEnergy += recoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        // エネルギー枯渇イベントの発火とフラグの管理
        if (currentEnergy <= 0.1f && !hasTriggeredEnergyDepletedEvent)
        {
            onEnergyDepleted?.Invoke();
            hasTriggeredEnergyDepletedEvent = true;
        }
        else if (currentEnergy > 0.1f && hasTriggeredEnergyDepletedEvent && Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            hasTriggeredEnergyDepletedEvent = false;
        }

        UpdateEnergyUI();
    }

    /// <summary>攻撃中のプレイヤーの状態を処理（移動ロックなど）</summary>
    void HandleAttackState()
    {
        if (!isAttacking) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackFixedDuration)
        {
            isAttacking = false;
            attackTimer = 0.0f;

            // 攻撃終了時: 接地していなければ重力の影響を受け始める
            if (!controller.isGrounded)
            {
                velocity.y = 0; // 攻撃中の固定を解除するが、急降下を防ぐために初期速度は0にする
            }
            else
            {
                velocity.y = -0.1f; // 接地判定を維持
            }
        }
    }

    // チュートリアル・UI関連のメソッド

    /// <summary>チュートリアル用の入力追跡フラグとタイマーをリセットする。</summary>
    public void ResetInputTracking()
    {
        WASDMoveTimer = JumpTimer = DescendTimer = 0f;
    }

    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    /// <summary>HPスライダーを更新する。</summary>
    void UpdateHPUI()
    {
        if (hPSlider != null)
        {
            // 現在HPを最大HPで割った値をスライダーの値として設定
            hPSlider.value = currentHP / maxHP;
        }
    }

    /// <summary>外部からダメージを受けたときに呼び出されます。</summary>
    public void TakeDamage(float damageAmount)
    {
        if (_currentArmorStats != null)
        {
            // ★アーマーの防御補正を適用したダメージ計算
            damageAmount *= _currentArmorStats.defenseMultiplier;
        }

        currentHP -= damageAmount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateHPUI();

        Debug.Log($"ダメージを受けました。残りHP: {currentHP}");

        if (currentHP <= 0)
        {
            Debug.Log("プレイヤーは破壊されました (Death Logic Here)");
            // 死亡処理をここに追加
        }
    }
}