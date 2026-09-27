using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Tetris2D.Audio;
using Tetris2D.Graficos;
using Tetris2D.UI;
using static Tetris2D.Audio.Sonido;

namespace Tetris2D.Pantallas
{
    internal class PantallaJuego : Pantalla
    {
       

        private const float IntervaloGravedadBase = 0.90f;

        private readonly string _nombreJugador;
        private readonly Random _rnd = new();

        private readonly tablero _tablero = new();

        private Pieza _piezaActual = null;
        private Pieza _piezaSiguiente = null;

        private int _posX;
        private int _posY;

        private float _contadorGraverdad;
        private int _puntaje;
        private int _lineas;
        private int _nivel = 1;

        private bool _gameover;
        private bool _pausa;


        private float _tableroX;
        private float _tableroY;
        private float _tamCelda;

        private float _panelX;
        private float _panelY;
        private float _panelW;
        private float _panelH;

        public PantallaJuego(GestorShader shaders, DibujadorCuadros cuadros, RenderizadorTexto texto, string nombreJugador) : base(shaders, cuadros, texto)
        {
            _nombreJugador = nombreJugador;

        }

        public override void Cargar()
        {
            base.Cargar();
            Reiniciar();
        }

        private void Reiniciar()
        {
            _tablero.Limpiar();
            _puntaje = 0;
            _lineas = 0;
            _nivel = 1;
            _gameover = false;
            _pausa = false;

            _piezaSiguiente = Pieza.Aleatoria(_rnd);
            GenerarPiezaNueva();
        }

        private void GenerarPiezaNueva()
        {
            _piezaActual = _piezaSiguiente;
            _piezaSiguiente = Pieza.Aleatoria(_rnd);
            _posX = tablero.columnas / 2 - 2;
            _posY = 0;
            _contadorGraverdad = 0f;

            if (_tablero.Colisiona(_piezaActual.Forma, _posX, _posY))
            {
                _gameover = true;
            }
        }
        private float IntevaloGraverdad => MathF.Max(0.85f, IntervaloGravedadBase * MathF.Pow(0.85f, _nivel - 1));

        public override void Actualizar(float dt, Vector2 raton)
        {
            base.Actualizar(dt, raton);
            if (_gameover || _pausa)
                return;

            _contadorGraverdad += dt;
            while (_contadorGraverdad >= IntevaloGraverdad)
            {
                _contadorGraverdad -= IntevaloGraverdad;
                if (!MoverPieza(0, 1))
                {
                    FijarYGenerar();
                    break;
                }
            }
        }
        private bool MoverPieza(int dx, int dy)
        {
            if (_tablero.Colisiona(_piezaActual.Forma, _posX + dx, _posY + dy))
            {
                return false;
            }
            _posX += dx;
            _posY += dy;
            return true;
        }
        public override void AlTecla(Keys tecla)
        {
            base.AlTecla(tecla);
            if (_gameover)
            {
                if (tecla == Keys.Enter)
                {
                    Reiniciar();
                    return;
                }
                if (tecla == Keys.Escape)
                {
                    _pausa = !_pausa;
                    return;
                }

                if (_pausa)
                    return;

            }
            switch (tecla)
            {
                case Keys.Left:
                    MoverPieza(-1, 0);
                    Sonido.Reproducir(TonoJuego.Mover);
                    break;
                case Keys.Right:
                    MoverPieza(1, 0);
                    Sonido.Reproducir(TonoJuego.Mover);
                    break;
                case Keys.Down:
                    if (!MoverPieza(0, 1))
                    {
                        _puntaje += 1;
                        Sonido.Reproducir(TonoJuego.Mover);
                    }
                    else
                    {
                        FijarYGenerar();
                    }
                    break;
                case Keys.Up:
                    HardDrop();
                    break;
                case Keys.Space:
                    RotarConEsquinas();
                    Sonido.Reproducir(TonoJuego.Rotar);
                    break;
                case Keys.P:
                    _pausa = !_pausa;
                    break;
            }
        }
        private bool RotarConEsquinas()
        {
            var rotacion = _piezaActual.OtenerRotacion(horario: true);

            int[] desplazamientos = { 0, -1, 1 };
            foreach (int dx in desplazamientos)
            {
                foreach (int dy in desplazamientos)
                {
                    if (!_tablero.Colisiona(rotacion, _posX + dx, _posY + dy))
                    {
                        _piezaActual.AplicarRotacion(horario: true);
                        _posX += dx;
                        return true;
                    }
                }
            }
            return false;
        }
        private void HardDrop()
        {
            int filas = 0;
            while (MoverPieza(0, 1))
            {
                filas++;
            }
            _puntaje += filas * 2;
            FijarYGenerar();

        }
        private void FijarYGenerar()
        {
            _tablero.FijarPieza(_piezaActual, _posX, _posY);
            int lineasBorradas = _tablero.LimpiarLineasCompletas();
            if (lineasBorradas > 0)
            {
                _lineas += lineasBorradas;
                int baseLineas = lineasBorradas switch
                {
                    1 => 100,
                    2 => 300,
                    3 => 500,
                    _ => 800
                };
                _puntaje += baseLineas * _nivel;

                int nuevoNivel = _lineas / 10 + 1;
                if (nuevoNivel > _nivel)
                {
                    _nivel = nuevoNivel
                    Sonido.Reproducir(TonoJuego.Nivel);
                }
                else
                {
                    Sonido.Reproducir(TonoJuego.Linea);
                }

                Sonido.Reproducir(TonoJuego.Fijar);

                nuevoNivel = _lineas / 10 + 1;

               
            }
            GenerarPiezaNueva();
        }
        public override void Renderizar(float ancho, float alto)
        {
            Cuadros.DibujarRectangulo(TemaArcade.Fondo, 0, 0, ancho, alto);
            CalcularDisposicion(ancho, alto);

            _tablero.Renderizar(Cuadros, _tableroX, _tableroY, _tamCelda);

            if (!_gameover)
                RenderizarPiezaViva();

            RenderizarPanelesLateral(ancho, alto);

            RenderizarEncabezado(ancho, alto);

            if (_pausa)
                RenderizarOverlayPausa(ancho, alto);

            if (_gameover)
                RenderizarGameOver(ancho, alto);
        }

        private void CalcularDisposicion(float ancho, float alto)
        {
            float margen = alto * 0.05f;
            float altoEncabezado = alto * 0.14f;

            float altoUtil = alto - altoEncabezado - margen;
            _tamCelda = MathF.Floor(altoUtil / tablero.filas);

            float anchoTablero = _tamCelda * tablero.columnas;
            float altoTablero = _tamCelda * tablero.filas;

            float separacion = _tamCelda * 1.6f;

            float anchoPanel = MathF.Max(140f, MathF.Min(ancho * 0.32f, ancho - anchoTablero - separacion - margen * 2f));

            float zonaTotal = anchoTablero + separacion + anchoPanel;
            _tableroX = MathF.Max(margen, (ancho - zonaTotal) * 0.5f);
            _tableroY = altoEncabezado + MathF.Max(0f, (altoUtil - altoTablero) * 0.5f);

            _panelX = _tableroX + anchoTablero + separacion;
            _panelY = _tableroY;
            _panelW = anchoPanel;
            _panelH = altoTablero;

        }

        private void RenderizarPiezaViva()
        {
            Vector4 bordeOscuro = new Vector4(0f, 0f, 0f, 0.35f);
            float grosorBisel = MathF.Max(1f, _tamCelda * 0.06f);

            foreach ((int bx, int by) in _piezaActual.Forma)
            {
                float px = _tableroX + (_posX + bx) * _tamCelda;
                float py = _tableroY + (_posY + by) * _tamCelda;

                Cuadros.DibujarRectangulo(_piezaActual.Color, px, py, px + _tamCelda, py + _tamCelda);
                Cuadros.DibujarBordeRectangulo(bordeOscuro, px, py, px + _tamCelda, py + _tamCelda, grosorBisel);
            }
        }
        private void RenderizarPanelesLateral(float ancho, float alto)
        {
            if (_panelW < 120f || _panelH < 120f)
                return;
            Cuadros.DibujarRectangulo(TemaArcade.PanelOscuro, _panelX, _panelY, _panelX + _panelW, _panelY + _panelH);
            float grosorBorde = MathF.Max(1f, _tamCelda * 0.06f);
            Cuadros.DibujarBordeRectangulo(TemaArcade.Cian, _panelX, _panelY, _panelX + _panelW, _panelY + _panelH, grosorBorde);

            float cx = _panelX + _panelW * 0.5f;
            float y = _panelY + _tamCelda * 0.5f;
            float avance = _tamCelda * 2.2f;

            TituloPanel("JUGADOR", cx, y, alto);
            float altoJugador = _tamCelda * 0.95f;
            string nombre = _nombreJugador.ToUpperInvariant();
            float wJugador = Texto.MedirTexto(nombre, altoJugador);
            Texto.DibujarTexto(nombre, cx - wJugador * 0.5f, y + avance * 0.32f, altoJugador, TemaArcade.Amarillo);

            float yPuntos = y + avance * 0.95f;
            TituloPanel("PUNTAJE", cx, yPuntos, alto);
            float valorPuntos = _tamCelda * 1.4f;
            string textoPuntos = _puntaje.ToString();
            float wPuntos = Texto.MedirTexto(textoPuntos, valorPuntos);
            Texto.DibujarTexto(textoPuntos, cx - wPuntos * 0.5f, yPuntos + avance * 0.30f, valorPuntos, TemaArcade.Amarillo);

            float yLineas = yPuntos + avance * 0.92f;
            TituloPanel("LINEAS", cx, yLineas, alto);
            float valorLineas = _tamCelda * 1.4f;
            string textoLineas = _lineas.ToString();
            float wLineas = Texto.MedirTexto(textoLineas, valorLineas);
            Texto.DibujarTexto(textoLineas, cx - wLineas * 0.5f, yLineas + avance * 0.30f, valorLineas, TemaArcade.Verde);

            float yNivel = yLineas + avance * 0.92f;
            TituloPanel("NIVEL", cx, yNivel, alto);
            float valorNivel = _tamCelda * 1.1f;
            string textoNivel = _nivel.ToString();
            float wNivel = Texto.MedirTexto(textoNivel, valorNivel);
            // Corregido: dibujar el texto del nivel, no el de las líneas
            Texto.DibujarTexto(textoNivel, cx - wNivel * 0.5f, yNivel + avance * 0.30f, valorNivel, TemaArcade.Rojo);

            float enNivel = _lineas % 10;
            string progreso = $"{enNivel}/10 PARA SUBIR";
            float altoProg = _tamCelda * 0.55f;
            float wProg = Texto.MedirTexto(progreso, altoProg);
            Texto.DibujarTexto(progreso, cx - wProg * 0.5f, yNivel + avance * 0.85f, altoProg, TemaArcade.TextoSuave);

            float ySig = yNivel + avance * 1.45f;
            TituloPanel("SIGUIENTE", cx, ySig, alto);
            float altoCaja = _tamCelda * 3.0f;
            float anchoCaja = MathF.Min(_panelW * 0.86f, _tamCelda * 4.2f);
            float cajaX = _panelX + (_panelW - anchoCaja) * 0.5f;
            float cajaY = ySig + avance * 0.30f;
            Cuadros.DibujarRectangulo(TemaArcade.PanelOscuro, cajaX, cajaY, cajaX + anchoCaja, cajaY + altoCaja);
            Cuadros.DibujarBordeRectangulo(TemaArcade.Cian, cajaX, cajaY, cajaX + anchoCaja, cajaY + altoCaja, grosorBorde);
            RenderizarPreview(_piezaSiguiente, cajaX, cajaY, anchoCaja, altoCaja);

            // Ayuda de controles al pie de la card.
            string controles = "← → MOVER ESPACIO ROTAR";
            string controles2 = "↓ BAJAR ARRIBA CAER";
            string controles3 = "ESC PAUSAR";
            float altoCtrl = _tamCelda * 0.55f;
            float wCtrl = Texto.MedirTexto(controles, altoCtrl);
            float wCtrl2 = Texto.MedirTexto(controles2, altoCtrl);
            float wCtrl3 = Texto.MedirTexto(controles3, altoCtrl);
            float yCtrl = _panelY + _panelH - _tamCelda * 3.4f;
            Texto.DibujarTexto(controles, cx - wCtrl * 0.5f, yCtrl, altoCtrl,
            TemaArcade.TextoSuave);
            Texto.DibujarTexto(controles2, cx - wCtrl2 * 0.5f, yCtrl + _tamCelda * 0.8f, altoCtrl,
            TemaArcade.TextoSuave);
            Texto.DibujarTexto(controles3, cx - wCtrl3 * 0.5f, yCtrl + _tamCelda * 1.6f, altoCtrl,
            TemaArcade.TextoSuave);
        }
        private void RenderizarPreview(Pieza pieza, float cajaX, float cajaY, float anchoCaja, float
        altoCaja)
        {
            if (pieza == null)
                return;
            // Bounding box de la pieza (min/max de sus bloques).
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach ((int bx, int by) in pieza.Forma)
            {
                minX = Math.Min(minX, bx); maxX = Math.Max(maxX, bx);
                minY = Math.Min(minY, by); maxY = Math.Max(maxY, by);
            }
            float tamMini = MathF.Min(anchoCaja, altoCaja) * 0.30f;
            float anchoPieza = (maxX - minX + 1) * tamMini;
            float altoPieza = (maxY - minY + 1) * tamMini;
            float px0 = cajaX + (anchoCaja - anchoPieza) * 0.5f;
            float py0 = cajaY + (altoCaja - altoPieza) * 0.5f;
            Vector4 bordeOscuro = new(0f, 0f, 0f, 0.35f);
            float grosorBisel = MathF.Max(1f, tamMini * 0.08f);
            foreach ((int bx, int by) in pieza.Forma)
            {
                float px = px0 + (bx - minX) * tamMini;
                float py = py0 + (by - minY) * tamMini;
                Cuadros.DibujarRectangulo(pieza.Color, px, py, px + tamMini, py + tamMini);
                Cuadros.DibujarBordeRectangulo(bordeOscuro, px, py, px + tamMini, py + tamMini,
                grosorBisel);
            }
        }
        private void RenderizarOverlayPausa(float ancho, float alto)
        {
            Cuadros.DibujarRectangulo(new Vector4(0f, 0f, 0f, 0.60f), 0, 0, ancho, alto);
            float cx = ancho * 0.5f;
            float panelAncho = ancho * 0.70f;
            float panelAlto = alto * 0.32f;
            float panelX = cx - panelAncho * 0.5f;
            float panelY = alto * 0.34f;
            Cuadros.DibujarRectangulo(TemaArcade.PanelOscuro, panelX, panelY, panelX + panelAncho,
            panelY + panelAlto);


            float grosorBorde = MathF.Max(2f, alto * 0.008f);
            Cuadros.DibujarBordeRectangulo(TemaArcade.Amarillo, panelX, panelY, panelX + panelAncho,
            panelY + panelAlto, grosorBorde);
            string titulo = "P A U S A";
            float altoTitulo = alto * 0.07f;
            float wTitulo = Texto.MedirTexto(titulo, altoTitulo);
            Texto.DibujarTexto(titulo, cx - wTitulo * 0.5f, panelY + panelAlto * 0.18f, altoTitulo,
            TemaArcade.Amarillo);
            string nota = "PRESIONA ESC PARA CONTINUAR";
            float altoNota = alto * 0.03f;
            float wNota = Texto.MedirTexto(nota, altoNota);
            Texto.DibujarTexto(nota, cx - wNota * 0.5f, panelY + panelAlto * 0.60f, altoNota,
            TemaArcade.TextoSuave);
        }
        private void TituloPanel(string texto, float cx, float y, float alto)
        {
            float altoTitulo = _tamCelda * 0.85f;
            float w = Texto.MedirTexto(texto, altoTitulo);
            Texto.DibujarTexto(texto, cx - w * 0.5f, y, altoTitulo, TemaArcade.Magenta);
        }
        private void RenderizarEncabezado(float ancho, float alto)
        {
            string titulo = "TETRIS — 2D";
            float altoTitulo = alto * 0.035f;
            float wTitulo = Texto.MedirTexto(titulo, altoTitulo);
            Texto.DibujarTexto(titulo, (ancho - wTitulo) * 0.5f, alto * 0.02f, altoTitulo,
            TemaArcade.Cian);
        }

        private void RenderizarGameOver(float ancho, float alto)
        {
            Cuadros.DibujarRectangulo(new Vector4(0f, 0f, 0f, 0.60f), 0, 0, ancho, alto);
            float cx = ancho * 0.5f;
            float panelAncho = ancho * 0.70f;
            float panelAlto = alto * 0.36f;
            float panelX = cx - panelAncho * 0.5f;
            float panelY = alto * 0.30f;
            Cuadros.DibujarRectangulo(TemaArcade.PanelOscuro, panelX, panelY, panelX + panelAncho, panelY + panelAlto);
            float grosorBorde = MathF.Max(2f, alto * 0.008f);
            Cuadros.DibujarBordeRectangulo(TemaArcade.Rojo, panelX, panelY, panelX + panelAncho, panelY + panelAlto, grosorBorde);
            string go = "GAME OVER";
            float altoGo = alto * 0.065f;
            float wGo = Texto.MedirTexto(go, altoGo);
            Texto.DibujarTexto(go, cx - wGo * 0.5f, panelY + panelAlto * 0.12f, altoGo, TemaArcade.Rojo);
            string resumen = $"PUNTAJE FINAL: {_puntaje}   •   LÍNEAS: {_lineas}";
            float altoResumen = alto * 0.033f;
            float wResumen = Texto.MedirTexto(resumen, altoResumen);
            Texto.DibujarTexto(resumen, cx - wResumen * 0.5f, panelY + panelAlto * 0.48f, altoResumen, TemaArcade.Amarillo);
            string reiniciar = "PRESIONA ENTER PARA JUGAR DE NUEVO";
            float altoRein = alto * 0.028f;
            float wRein = Texto.MedirTexto(reiniciar, altoRein);
            Texto.DibujarTexto(reiniciar, cx - wRein * 0.5f, panelY + panelAlto * 0.72f, altoRein, TemaArcade.TextoSuave);
        }





    }

    }

