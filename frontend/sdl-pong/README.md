# SDL3 Pong Sample

A small Pong game written in Myll that compiles to C++ and links against SDL3.

## Build

Requires `libsdl3-dev`:

```bash
sudo apt-get install libsdl3-dev
```

From this directory:

```bash
make
```

This runs the Myll compiler and then compiles the generated C++ with `g++`.

## Run

```bash
./gen/pong
```

Controls:

- Left paddle: W / S
- Right paddle: Up / Down

The game runs for about 10 seconds or until the window is closed.

## Headless / CI

To run without a display:

```bash
SDL_VIDEODRIVER=dummy ./gen/pong
```
