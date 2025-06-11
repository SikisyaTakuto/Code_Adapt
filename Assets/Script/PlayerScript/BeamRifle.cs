using UnityEngine;

public class BeamRifle : MonoBehaviour
{
    public float range = 100f;            // �r�[���̎˒�����
    public float damage = 25f;            // �_���[�W��
    public Camera fpsCamera;              // �v���C���[�̃J�����i�ː��̋N�_�j
    public ParticleSystem muzzleFlash;   // ���ˎ��̃G�t�F�N�g�i�C�Ӂj
    public GameObject impactEffectPrefab; // �q�b�g���̃G�t�F�N�g�i�C�Ӂj

    void Update()
    {
        if (Input.GetButtonDown("Fire1")) // �}�E�X���N���b�N��R���g���[���̃g���K�[�Ȃ�
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // �G�t�F�N�g�Đ�
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        RaycastHit hit;
        Vector3 origin = fpsCamera.transform.position;
        Vector3 direction = fpsCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);

            // �q�b�g�G�t�F�N�g�𐶐�
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            //// �q�b�g��������Ƀ_���[�W��^����iHealth�X�N���v�g�ȂǂɃA�N�Z�X�j
            //var health = hit.transform.GetComponent<Health>();
            //if (health != null)
            //{
            //    health.TakeDamage(damage);
            //}
        }
    }
}
