using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WishBound.Mobile.ViewModels
{
    /// <summary>
    /// Base de todos os view models: avisa o ecrã quando uma propriedade muda
    /// (INotifyPropertyChanged), que é o que faz os {Binding} atualizarem.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _ocupado;

        /// <summary>true enquanto há um pedido à API a decorrer.</summary>
        public bool Ocupado
        {
            get => _ocupado;
            set
            {
                if (Definir(ref _ocupado, value))
                {
                    Notificar(nameof(NaoOcupado));
                }
            }
        }

        public bool NaoOcupado => !_ocupado;

        protected bool Definir<T>(ref T campo, T valor, [CallerMemberName] string? nome = null)
        {
            if (EqualityComparer<T>.Default.Equals(campo, valor))
            {
                return false;
            }

            campo = valor;
            Notificar(nome);
            return true;
        }

        protected void Notificar([CallerMemberName] string? nome = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }
    }
}
