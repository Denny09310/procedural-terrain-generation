let canvas, ctx, dotNetRef, chunkSize;
let zoom = 1.0;
let panX = 0;
let panY = 0;
let isDragging = false;
let startX = 0;
let startY = 0;

const chunkCache = new Map();      // Key: "x,y" -> Value: ImageBitmap
const requestedChunks = new Set(); // Tracks active Blazor interop fetches

export function init(canvasElement, componentRef, size) {
    canvas = canvasElement;
    ctx = canvas.getContext("2d");
    dotNetRef = componentRef;
    chunkSize = size;

    resizeCanvas();
    window.addEventListener("resize", resizeCanvas);

    // Pointer events handle both Mouse and Touch inputs automatically
    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", onPointerUp);
    canvas.addEventListener("pointercancel", onPointerUp);
    canvas.addEventListener("wheel", onWheel, { passive: false });

    // Kick off the 60 FPS  ing cycle
    requestAnimationFrame(renderLoop);
}

function resizeCanvas() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}

// --- Interaction Handlers ---
function onPointerDown(e) {
    isDragging = true;
    canvas.setPointerCapture(e.pointerId);
    startX = e.clientX - panX;
    startY = e.clientY - panY;
}

function onPointerMove(e) {
    if (!isDragging) return;
    panX = e.clientX - startX;
    panY = e.clientY - startY;
}

function onPointerUp(e) {
    if (!isDragging) return;
    isDragging = false;
    canvas.releasePointerCapture(e.pointerId);
}

function onWheel(e) {
    e.preventDefault();
    const zoomFactor = 1.15;
    const mouseX = e.clientX;
    const mouseY = e.clientY;

    // Pinpoint what world coordinate the mouse is hovering over before zooming
    const worldX = (mouseX - panX) / zoom;
    const worldY = (mouseY - panY) / zoom;

    if (e.deltaY < 0) {
        zoom *= zoomFactor;
    } else {
        zoom /= zoomFactor;
    }

    // Cap zoom limits
    zoom = Math.max(0.05, Math.min(zoom, 15.0));

    // Readjust pan coordinates so the mouse pointer remains anchored to the same world spot
    panX = mouseX - worldX * zoom;
    panY = mouseY - worldY * zoom;
}

// --- Dynamic Render Loop ---
function renderLoop() {
    if (!canvas) return;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.save();
    ctx.translate(panX, panY);
    ctx.scale(zoom, zoom);

    // Calculate reverse matrix bounds to find out exactly what chunks are visible on screen
    const minWorldX = -panX / zoom;
    const maxWorldX = (canvas.width - panX) / zoom;
    const minWorldY = -panY / zoom;
    const maxWorldY = (canvas.height - panY) / zoom;

    const minChunkX = Math.floor(minWorldX / chunkSize);
    const maxChunkX = Math.floor(maxWorldX / chunkSize);
    const minChunkY = Math.floor(minWorldY / chunkSize);
    const maxChunkY = Math.floor(maxWorldY / chunkSize);

    // Render loop only processes chunks entering the viewport matrix
    for (let cy = minChunkY; cy <= maxChunkY; cy++) {
        for (let cx = minChunkX; cx <= maxChunkX; cx++) {
            const key = `${cx},${cy}`;

            if (chunkCache.has(key)) {
                ctx.drawImage(chunkCache.get(key), cx * chunkSize, cy * chunkSize);
            } else if (!requestedChunks.has(key)) {
                requestedChunks.add(key);
                startRendering(cx, cy);
            }
        }
    }

    ctx.restore();
    requestAnimationFrame(renderLoop);
}

function startRendering(cx, cy) {
    dotNetRef.invokeMethodAsync("StartRendering", cx, cy)
        .catch(err => console.error(`Error initiating chunk fetch [${cx}, ${cy}]:`, err));
}

export function endRendering(cx, cy, biomes, structures) {
    if (biomes) {
        createChunkBitmap(biomes, structures).then(bitmap => {
            chunkCache.set(`${cx},${cy}`, bitmap);
        }).catch(err => {
            console.error(`Error processing bitmap for [${cx}, ${cy}]:`, err);
        });
    }
}

async function createChunkBitmap(biomes, structures) {
    const totalCells = chunkSize * chunkSize;
    const buffer = new Uint8ClampedArray(totalCells * 4);

    for (let i = 0; i < totalCells; i++) {
        let color = getBiomeColor(biomes[i]);

        if (structures && structures[i] > 0) {
            color = getStructureColor(structures[i]);
        }

        const offset = i * 4;
        buffer[offset] = color.r;
        buffer[offset + 1] = color.g;
        buffer[offset + 2] = color.b;
        buffer[offset + 3] = 255;
    }

    const imageData = new ImageData(buffer, chunkSize, chunkSize);
    return await createImageBitmap(imageData);
}

export function clearCache() {
    chunkCache.forEach(bitmap => bitmap.close());
    chunkCache.clear();
    requestedChunks.clear();
}

export function dispose() {
    window.removeEventListener("resize", resizeCanvas);
    clearCache();
    canvas = null;
    ctx = null;
}

function getBiomeColor(biome) {
    switch (biome) {
        case 0: return { r: 0, g: 105, b: 148 };   // Water (Deep Blue)
        case 1: return { r: 30, g: 144, b: 255 };  // River (Dodger Blue)
        case 2: return { r: 238, g: 214, b: 175 }; // Beach (Sand)
        case 3: return { r: 210, g: 180, b: 140 }; // Desert (Tan/Desert Sand)
        case 4: return { r: 50, g: 205, b: 50 };   // Plains (Lime Green)
        case 5: return { r: 34, g: 139, b: 34 };   // Forest (Forest Green)
        case 6: return { r: 120, g: 120, b: 120 }; // Hills (Light Gray)
        case 7: return { r: 90, g: 90, b: 90 };    // Mountains (Dark Rock Gray)
        case 8: return { r: 255, g: 255, b: 255 }; // Snow (Pure White)
        default: return { r: 255, g: 0, b: 255 };  // Error Fallback (Magenta)
    }
}

function getStructureColor(structure) {
    switch (structure) {
        case 1: return { r: 220, g: 20, b: 60 };   // CityCenter (Crimson Red Hub)
        case 2: return { r: 139, g: 69, b: 19 };   // House (Saddle Brown Roof)
        case 3: return { r: 105, g: 105, b: 105 };  // Road (Dim Gray Gravel)
        default: return { r: 255, g: 255, b: 0 };   // Unknown/Fallback (Yellow)
    }
}