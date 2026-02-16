export function initSlider(container) {
    if (!container) return;

    const handle = container.querySelector('.slider-handle');
    const foreground = container.querySelector('.slider-foreground');

    if (!handle || !foreground) return;

    let isDragging = false;

    // Use formatting for easier debugging
    function update(clientX) {
        const rect = container.getBoundingClientRect();
        let offsetX = clientX - rect.left;

        // Clamp
        if (offsetX < 0) offsetX = 0;
        if (offsetX > rect.width) offsetX = rect.width;

        const percentage = (offsetX / rect.width) * 100;

        foreground.style.width = `${percentage}%`;
        handle.style.left = `${percentage}%`;
    }

    const onMove = (e) => {
        if (!isDragging) return;

        // Handle both mouse and touch
        const clientX = e.touches ? e.touches[0].clientX : e.clientX;
        update(clientX);
    };

    const onUp = () => {
        isDragging = false;
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('touchmove', onMove);
        document.removeEventListener('mouseup', onUp);
        document.removeEventListener('touchend', onUp);
    };

    const onDown = (e) => {
        isDragging = true;

        // Initial update on click
        const clientX = e.touches ? e.touches[0].clientX : e.clientX;
        update(clientX);

        document.addEventListener('mousemove', onMove);
        document.addEventListener('touchmove', onMove);
        document.addEventListener('mouseup', onUp);
        document.addEventListener('touchend', onUp);

        // Prevent default only if needed (e.g., prevent scrolling on touch)
        // e.preventDefault(); 
    };

    container.addEventListener('mousedown', onDown);
    container.addEventListener('touchstart', onDown, { passive: false });
}
