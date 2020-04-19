using System.Windows.Input;

namespace Models
{
    public abstract class Utilizador
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public ICommand AbrirSeparadorChatCommand { get; set; }
    }
}