using UnityEngine;

// Marca o ponto de respawn do player na cena. GameManager (DontDestroyOnLoad)
// guarda uma referência Inspector pro Transform, mas isso é um objeto scene-local
// — depois de um reload de cena (EnterWorld) o objeto antigo é destruído e a
// referência fica "fake-null". Esse marcador deixa o GameManager reencontrar o
// Transform correto no novo objeto, via Instance, em vez de cair silenciosamente
// em Vector3.zero (ver GameManager.InitializeSceneBootstrap).
public class PlayerRespawnPoint : MonoBehaviour
{
    public static PlayerRespawnPoint Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
