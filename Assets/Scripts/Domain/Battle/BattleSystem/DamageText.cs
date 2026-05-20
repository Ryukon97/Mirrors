using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    private TextMeshPro textMesh;
    [SerializeField] private float moveSpeed = 1.5f;     // 위로 떠오르는 속도
    [SerializeField] private float fadeSpeed = 2.0f;     // 투명해지는 속도
    [SerializeField] private float disappearTimer = 0.4f; // 완전히 투명해지기 전 버티는 시간
    private Color textColor;

    public void Setup(int damageAmount)
    {
        // 3D 공간에 배치될 텍스트이므로 TextMeshProUGUI가 아닌 TextMeshPro를 사용합니다.
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();

        if (textMesh != null)
        {
            textMesh.text = damageAmount.ToString();
            textColor = textMesh.color;
        }
    }

    private void Update()
    {
        // 1. 위로 서서히 이동
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // 2. 타이머가 끝나면 페이드아웃 후 오브젝트 파괴
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            textColor.a -= fadeSpeed * Time.deltaTime;
            if (textMesh != null) textMesh.color = textColor;

            if (textColor.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}