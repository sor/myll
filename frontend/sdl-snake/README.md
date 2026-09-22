# SDL3 Snake Sample

A small Snake game written in Myll that compiles to C++ and links against SDL3.

## Build

Requires `libsdl3-dev`:

```bash
sudo apt-get install libsdl3-dev
```

From this directory:

```bash
make
```

## Run

```bash
./gen/snake
```

Controls:

- Arrow keys to steer

The game exits when the snake hits a wall or itself, when the window is closed,
or after a fixed number of frames.

## Headless / CI

To run without a display:

```bash
SDL_VIDEODRIVER=dummy ./gen/snake
```
