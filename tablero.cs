using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using Tetris2D.Graficos;
using Tetris2D.UI;
using static Tetris2D.Pieza;

namespace Tetris2D
{
    internal class tablero
    {
        public const int columnas = 10;
        public const int filas = 20;

        private readonly int[,] _celdas = new int[columnas, filas];

        public int ObtenerCelda(int x, int y)
        {
            return _celdas[x, y];
        }

        public void Limpiar()
        {
            for (int x = 0; x < columnas; x++)
                for (int y = 0; y < filas; y++)
                    _celdas[x, y] = 0;
        }

        public bool Colisiona(IReadOnlyList<(int X, int Y)> forma, int posX, int posY)
        {
            foreach ((int x, int y) in forma)
            {
                int tableroX = posX + x;
                int tableroY = posY + y;
                if (tableroX < 0 || tableroX >= columnas || tableroY < 0 || tableroY >= filas)
                    return true; // Fuera de los limites del tablero
                if (_celdas[tableroX, tableroY] != 0)
                    return true; // Colision con una celda ocupada
            }
            return false; // No hay colision
        }

        public void FijarPieza(Pieza pieza,int PosX,int PosY)
        {
            int valorColor = (int)pieza.Tipo + 1;
            foreach ((int x, int y) in pieza.Forma)
            {
                int tableroX = PosX + x;
                int tableroY = PosY + y;
                if (tableroX >= 0 && tableroX < columnas && tableroY >= 0 && tableroY < filas)
                {
                    _celdas[tableroX, tableroY] = valorColor;
                }
            }
        }

        public int LimpiarLineasCompletas()
        {
            int lineasBorradas = 0;
            for(int y = filas - 1; y >= 0; y--)
            {
                if (FilaCompleta(y))
                {
                    lineasBorradas++;
                    BajarFilasSuperiores(y);
                    y++;

                }
            }
            return lineasBorradas;
        }

        private bool FilaCompleta(int y)
        {
            for(int x = 0; x < columnas; x++)
            {
                if (_celdas[x, y] == 0)
                    return false;
            }
            return true;
        }

        private void BajarFilasSuperiores(int filaBorrada)
        {
            for(int y = filaBorrada; y > 0; y--)
            {
                for(int x = 0; x < columnas; x++)
                {
                    _celdas[x, y] = _celdas[x, y - 1];
                }
            }
            // Limpiar la primera fila
            for(int x = 0; x < columnas; x++)
            {
                _celdas[x, 0] = 0;
            }
        }
        public void Renderizar(DibujadorCuadros cuadros, float x0, float y0, float tamCelda)
        {
            float ancho = columnas * tamCelda;
            float alto = filas * tamCelda;
            // Fondo oscuro del area de juego + borde neon.
            cuadros.DibujarRectangulo(TemaArcade.PanelOscuro, x0, y0, x0 + ancho, y0 + alto);
            float grosorBorde = MathF.Max(2f, tamCelda * 0.10f);
            cuadros.DibujarBordeRectangulo(TemaArcade.Cian, x0, y0, x0 + ancho, y0 + alto,
            grosorBorde);

            // Cuadricula líneas verticales y horizontales cada tamCelda).
            Vector4 colorCuadricula = new(TemaArcade.TextoSuave.X, TemaArcade.TextoSuave.Y,
            TemaArcade.TextoSuave.Z, 0.10f);
            for (int x = 1; x < columnas; x++)
            {
                float lx = x0 + x * tamCelda;
                cuadros.DibujarLinea(colorCuadricula, lx, y0, lx, y0 + alto, 1f);
            }
            for (int y = 1; y < filas; y++)
            {
                float ly = y0 + y * tamCelda;
                cuadros.DibujarLinea(colorCuadricula, x0, ly, x0 + ancho, ly, 1f);
            }

            // Cada bloque fijado: rectangulo de su color con un borde interior
            // oscuro para dar volumen ("bisel").
            Vector4 bordeOscuro = new(0f, 0f, 0f, 0.35f);
            float grosorBisel = MathF.Max(1f, tamCelda * 0.06f);
            for (int x = 0; x < columnas; x++)
            {
                for (int y = 0; y < filas; y++)
                {
                    int valor = _celdas[x, y];
                    if (valor == 0)
                        continue;
                    Vector4 color = PiezaColor(valor);
                    float px = x0 + x * tamCelda;
                    float py = y0 + y * tamCelda;
                    cuadros.DibujarRectangulo(color, px, py, px + tamCelda, py + tamCelda);
                    cuadros.DibujarBordeRectangulo(bordeOscuro, px, py, px + tamCelda, py +
                    tamCelda, grosorBisel);
                }
            }
        }
        private static Vector4 PiezaColor(int valorCelda)
        {
            TipoPieza tipo = (TipoPieza)(valorCelda - 1);
            return new Pieza(tipo).Color;
        }
    }
}

