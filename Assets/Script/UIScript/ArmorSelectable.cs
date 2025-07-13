// ArmorSelectable.cs
using UnityEngine;
using UnityEngine.EventSystems; // �}�E�X�C�x���g���������߂ɕK�v
using System.Collections;       // Coroutine�̂��߂ɕK�v

// �K�v�ȃC�x���g�C���^�[�t�F�[�X�����ׂĎ������Ă��邱�Ƃ��m�F
public class ArmorSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public ArmorData armorData; // ���̃A�[�}�[���f���Ɋ֘A�t����ꂽ�f�[�^
    public ArmorSelectUI armorSelectUI; // �e��ArmorSelectUI�ւ̎Q��

    private Vector3 originalScale;
    private Color originalColor;
    private Renderer _renderer; // ���f����Renderer (��������ꍇ��InChildren�Ŏ擾)

    private const float HOVER_SCALE_FACTOR = 1.1f; // �}�E�X�I�[�o�[���̊g�嗦
    private const float CLICK_SCALE_FACTOR = 1.05f; // �N���b�N���̊g�嗦 (���̃X�P�[���)
    private const float ANIMATION_DURATION = 0.1f; // �A�j���[�V��������

    private bool _isBeingHeld = false; // �}�E�X�������ꑱ���Ă��邩

    void Awake()
    {
        originalScale = transform.localScale;
        _renderer = GetComponentInChildren<Renderer>(); // ���ύX�_: �q��Renderer���l��
        if (_renderer != null)
        {
            // �}�e���A���̃C���X�^���X���쐬���A�I���W�i���̐F��ێ�
            _renderer.material = new Material(_renderer.material); // ���ύX�_: �}�e���A���̃C���X�^���X���쐬
            originalColor = _renderer.material.color;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Renderer��������܂���B�F�ύX�͖����ł��B", this);
        }

        // �R���C�_�[�����݂��邩�m�F���A�Ȃ���Βǉ�
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>(); // ���ύX�_: BoxCollider��ǉ�
            Debug.LogWarning($"{gameObject.name}: Collider������܂���ł����BBoxCollider��ǉ����܂����B", this);
        }
        // Collider���g���K�[�ɐݒ肵�āA���f���������I�Ɋ����Ȃ��悤�ɂ���
        collider.isTrigger = true;

        // Renderer����Bound�����擾����BoxCollider�̃T�C�Y����������
        if (_renderer != null && collider is BoxCollider boxCollider)
        {
            // Renderer��Bounds�̃��[�J�����W���v�Z����Collider�ɐݒ�
            Vector3 center = _renderer.bounds.center - transform.position;
            Vector3 size = _renderer.bounds.size;
            boxCollider.center = center;
            boxCollider.size = size;
            Debug.Log($"{gameObject.name}: BoxCollider�̃T�C�Y��Renderer�ɍ��킹�Ē������܂����B");
        }
    }

    // �}�E�X�J�[�\�������f���ɓ�������
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (armorSelectUI != null)
        {
            armorSelectUI.ShowDescription(armorData); // �����p�l����\��
        }
        // �}�E�X�I�[�o�[�A�j���[�V����
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
    }

    // �}�E�X�J�[�\�������f������o����
    public void OnPointerExit(PointerEventData eventData)
    {
        // �X�P�[�������ɖ߂�
        if (!_isBeingHeld) // �N���b�N���̓X�P�[�����ێ�
        {
            LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
        }
    }

    // �}�E�X�{�^���������ꂽ��
    public void OnPointerDown(PointerEventData eventData)
    {
        _isBeingHeld = true;
        // �}�E�X�_�E�����̃A�j���[�V����
        LeanTween.scale(gameObject, originalScale * CLICK_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();

        // �I������Ă���A�[�}�[��Highlight���X�V
        if (armorSelectUI != null)
        {
            armorSelectUI.HighlightTemporary(armorData); // ���ύX�_: �ꎞ�I�ȃn�C���C�g���Ăяo��
        }
    }

    // �}�E�X�{�^���������ꂽ��
    public void OnPointerUp(PointerEventData eventData)
    {
        _isBeingHeld = false;
        // �}�E�X�A�b�v���̃A�j���[�V����
        // �}�E�X���܂����f����ɂ���ꍇ��HOVER_SCALE_FACTOR�ɖ߂�
        if (EventSystem.current.IsPointerOverGameObject() && eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
        }
        else
        {
            LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
        }

        // �A�[�}�[�I�����W�b�N���Ăяo��
        if (armorSelectUI != null)
        {
            armorSelectUI.OnArmorClicked(armorData); // �I��/�������W�b�N���Ăяo��
        }
    }

    // ���f���̐F���n�C���C�g/�ʏ�ɖ߂�
    public void SetHighlight(bool isSelected) // ���ύX�_: ���\�b�h����isSelected�ɕύX�i�I���ςݏ�Ԃ𖾊m���j
    {
        if (_renderer != null)
        {
            _renderer.material.color = isSelected ? Color.yellow : originalColor; // ��: ���F�Ńn�C���C�g
        }
    }

    // �ꎞ�I�ȃn�C���C�g�i�}�E�X�_�E�����̂݁j
    public void SetTemporaryHighlight(bool isTemporaryHighlighted)
    {
        if (_renderer != null)
        {
            // ���ɑI���ς݂̏ꍇ�͐F��ς��Ȃ��i�I���ς݂��D��j
            if (armorSelectUI != null && armorSelectUI.IsArmorSelected(armorData))
            {
                _renderer.material.color = Color.yellow; // �I���ς݂͏�ɉ��F
            }
            else
            {
                _renderer.material.color = isTemporaryHighlighted ? Color.cyan : originalColor; // ��: �}�E�X�_�E�����̓V�A��
            }
        }
    }
}