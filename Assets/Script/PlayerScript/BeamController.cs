using UnityEngine;
using System.Collections;

public class BeamController : MonoBehaviour
{
    // �r�[���G�t�F�N�g�̃��[�g�ɂ���S�Ă�Particle System
    private ParticleSystem[] particles;

    // �y�ݒ荀�ځz�r�[���̕\������
    [Header("�r�[���̎�������")]
    public float beamDuration = 0.1f;

    void Awake()
    {
        // �Q�[���I�u�W�F�N�g�Ƃ��̑S�Ă̎q�I�u�W�F�N�g����ParticleSystem���擾
        particles = GetComponentsInChildren<ParticleSystem>();

        if (particles.Length == 0)
        {
            Debug.LogError("BeamController: �q���܂�ParticleSystem��������܂���B");
            enabled = false;
        }

        // ������ԂŃG�t�F�N�g���~���Ă���
        foreach (var ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>�r�[���𔭎˂��A�����Ɏ������ŏ������J�n���܂��B</summary>
    public void Fire(Vector3 startPoint, Vector3 endPoint)
    {
        // Line Renderer���Ȃ����߁A�r�[���̈ʒu�ƕ�����e�I�u�W�F�N�g�Őݒ肵�܂�
        // startPoint (���ˌ�) �����̃Q�[���I�u�W�F�N�g�̈ʒu�Ƃ��Đݒ肵�܂�
        transform.position = startPoint;

        // ������ݒ� (endPoint�����m�Ȓ��e�_�̏ꍇ)
        // �r�[����startPoint����endPoint�������悤�ɉ�]������
        Vector3 direction = endPoint - startPoint;
        transform.rotation = Quaternion.LookRotation(direction);

        // �� Particle System������ "Shape" ���W���[���̐ݒ�ŁA
        // �r�[�����K�؂�startPoint����endPoint�܂ŐL�т�悤�ɒ������K�v�ł��B

        // 1. �S�Ă�Particle System���Đ��J�n
        foreach (var ps in particles)
        {
            ps.Play();
        }

        // 2. �ݒ肳�ꂽ���Ԍ�ɂ��̃Q�[���I�u�W�F�N�g�S�̂�j������
        Destroy(gameObject, beamDuration);
    }
}